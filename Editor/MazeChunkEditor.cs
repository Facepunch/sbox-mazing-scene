using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;
using static System.Net.Mime.MediaTypeNames;

namespace Mazing.Editor;

[CustomEditor( typeof( MazeData ) )]
public class MazeChunkWidget : ControlWidget
{
	public override bool IsWideMode => true;

	public override bool SupportsMultiEdit => false;

	public override bool IncludeLabel => false;

	private Layout Content { get; }

	protected override int ValueHash => SerializedProperty.GetValue<MazeData>().ValueHash;

	private int _width;
	private int _height;

	private MazeChunkCellWidget[,]? _cells;

	private WallState? _wallStateBrush;

	public MazeChunkWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		Layout.Margin = 16f;

		Content = Layout.Column();
		Content.Margin = 4f;

		Layout.Add( Content );

		Rebuild();
	}

	public override void ChildValuesChanged( Widget source )
	{
		base.ChildValuesChanged( source );

		// Do I have to do this?

		if ( SerializedProperty.TryGetAsObject( out var obj ) )
		{
			obj.NoteChanged( SerializedProperty );
		}

		var data = SerializedProperty.GetValue<MazeData>();

		if ( _width != data.Width || _height != data.Height )
		{
			Rebuild();
		}
	}

	[EditorEvent.Hotload]
	private void Rebuild()
	{
		Content.Clear( true );

		var data = SerializedProperty.GetValue<MazeData>();

		_width = data.Width;
		_height = data.Height;

		SerializedProperty.TryGetAsObject( out var obj );

		var controlSheet = new ControlSheet();

		Content.Add( controlSheet );

		controlSheet.AddRow( obj.GetProperty( nameof( MazeData.Width) ) );
		controlSheet.AddRow( obj.GetProperty( nameof( MazeData.Height) ) );

		var grid = Layout.Grid();
		grid.HorizontalSpacing = 0;
		grid.VerticalSpacing = 0;
		grid.Alignment = TextFlag.Left;
		grid.Margin = 16f;

		_cells = new MazeChunkCellWidget[data.Height, data.Width];

		for ( var i = 0; i < data.Height; ++i )
		{
			for ( var j = 0; j < data.Width; ++j )
			{
				var cell = new MazeChunkCellWidget( this, data, i, j );

				_cells[i, j] = cell;

				grid.AddCell( j, i, cell );
			}
		}

		Content.Add( grid );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		_wallStateBrush = e.LeftMouseButton ? WallState.Closed : WallState.Open;
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		_wallStateBrush = null;
	}

	private bool TryGetCellPos( Vector2 localPos, out int row, out int col, out Vector2 cellLocalPos )
	{
		var offset = _cells![0, 0].Position;

		localPos -= offset;

		row = (int)MathF.Floor( localPos.y / 32f );
		col = (int)MathF.Floor( localPos.x / 32f );

		cellLocalPos = localPos - new Vector2( col, row ) * 32f;

		return row >= 0 && row < _height && col >= 0 && col < _width;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );

		if ( _wallStateBrush is null || _cells is null )
		{
			return;
		}

		if ( TryGetCellPos( e.LocalPosition, out var row, out var col, out var cellLocalPos ) )
		{
			_cells[row, col].PaintWall( cellLocalPos, _wallStateBrush.Value );
		}
	}

	protected override void PaintUnder()
	{
		Paint.ClearPen();
		Paint.SetBrush( Theme.WidgetBackground.Darken( 0.1f ) );

		Paint.DrawRect( ContentRect.Shrink( 8f ) );
	}
}

internal class MazeChunkCellWidget : Widget
{
	public MazeChunkWidget MazeChunkWidget { get; }
	public MazeData Data { get; }
	public int Row { get; }
	public int Col { get; }

	public MazeChunkCellWidget( MazeChunkWidget parent, MazeData data, int row, int col )
		: base( parent )
	{
		MazeChunkWidget = parent;

		Data = data;
		Row = row;
		Col = col;

		FixedSize = new Vector2( 32f, 32f );

		ToolTip = $"{Row + 1}, {Col + 1}";
	}

	private Direction? GetDirection( Vector2 localPos )
	{
		var pos = (localPos / Size - 0.5f) * 2f;

		if ( MathF.Abs( pos.x ) < 0.5f == MathF.Abs( pos.y ) < 0.5f )
		{
			return null;
		}

		if ( MathF.Abs( pos.x ) > MathF.Abs( pos.y ) )
		{
			return pos.x <= 0f ? Direction.West : Direction.East;
		}

		return pos.y <= 0f ? Direction.North : Direction.South;
	}

	public void PaintWall( Vector2 localPos, WallState state )
	{
		if ( GetDirection( localPos ) is not { } dir )
		{
			return;
		}

		var prev = Data[Row, Col, dir];

		if ( prev == state )
		{
			return;
		}

		Data[Row, Col, dir] = state;

		Update();
		SignalValuesChanged();
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		if ( GetDirection( e.LocalPosition ) is not null )
		{
			return;
		}

		var prev = Data[Row, Col];
		var next = (CellState)(((int)prev + 1) % (int)CellState.Count);

		Data[Row, Col] = next;

		Update();
		SignalValuesChanged();
	}

	protected override void OnMouseRightClick( MouseEvent e )
	{
		base.OnMouseRightClick( e );

		if ( GetDirection( e.LocalPosition ) is not null )
		{
			return;
		}

		var prev = Data[Row, Col];
		var next = (CellState)(((int)prev + (int)CellState.Count - 1) % (int)CellState.Count);

		Data[Row, Col] = next;

		Update();
		SignalValuesChanged();
	}

	private static Dictionary<CellState, string>? CellStateIcons { get; set; }

	private static string? GetIcon( CellState state )
	{
		CellStateIcons ??= TypeLibrary.GetEnumDescription( typeof( CellState ) )
			.Where( x => x.Icon != null )
			.ToDictionary( x => (CellState)x.ObjectValue, x => x.Icon );

		return CollectionExtensions.GetValueOrDefault( CellStateIcons, state );
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;

		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlText.Darken( 0.8f ) );

		if ( Data[Row, Col] != CellState.Empty )
		{
			Paint.DrawRect( ContentRect.Shrink( 1f ) );
		}

		Paint.SetBrush( Theme.ControlText.Darken( 0.2f ) );

		if ( Data[Row, Col, Direction.West] == WallState.Closed )
		{
			Paint.DrawRect( new Rect( ContentRect.Left, ContentRect.Top, 2f, ContentRect.Height ) );
		}

		if ( Data[Row, Col, Direction.East] == WallState.Closed )
		{
			Paint.DrawRect( new Rect( ContentRect.Right - 2f, ContentRect.Top, 2f, ContentRect.Height ) );
		}

		if ( Data[Row, Col, Direction.North] == WallState.Closed )
		{
			Paint.DrawRect( new Rect( ContentRect.Left, ContentRect.Top, ContentRect.Width, 2f ) );
		}

		if ( Data[Row, Col, Direction.South] == WallState.Closed )
		{
			Paint.DrawRect( new Rect( ContentRect.Left, ContentRect.Bottom - 2f, ContentRect.Width, 2f ) );
		}

		if ( GetIcon( Data[Row, Col] ) is {} icon )
		{
			Paint.SetPen( Theme.ControlText );
			Paint.DrawIcon( ContentRect.Shrink( 4f ), icon, 20f );
		}
	}
}
