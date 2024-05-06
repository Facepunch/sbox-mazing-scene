using System;
using Editor;
using Mazing.Resources;
using Sandbox;

namespace Mazing.Editor;

[CustomEditor( typeof( MazeChunkData ) )]
public class MazeChunkWidget : ControlWidget
{
	public override bool IsWideMode => true;

	public override bool SupportsMultiEdit => false;

	public override bool IncludeLabel => false;

	private Layout Content { get; }

	protected override int ValueHash => SerializedProperty.GetValue<MazeChunkData>().ValueHash;

	public MazeChunkWidget( SerializedProperty property ) : base( property )
	{
		Layout = Layout.Column();
		Layout.Spacing = 2;
		Layout.Margin = 16f;

		Content = Layout.Column();

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
	}

	[EditorEvent.Hotload]
	private void Rebuild()
	{
		Content.Clear( true );
		Content.Margin = 4f;

		var grid = Layout.Grid();
		grid.HorizontalSpacing = 0;
		grid.VerticalSpacing = 0;
		grid.Alignment = TextFlag.Left;

		var data = SerializedProperty.GetValue<MazeChunkData>();

		for ( var i = 0; i < data.Height; ++i )
		{
			for ( var j = 0; j < data.Width; ++j )
			{
				var cell = new MazeChunkCellWidget( this, data, i, j );

				grid.AddCell( j, i, cell );
			}
		}

		Content.Add( grid );
	}

	protected override void PaintUnder()
	{

	}
}

internal class MazeChunkCellWidget : Widget
{
	public MazeChunkData Data { get; }
	public int Row { get; }
	public int Col { get; }

	public MazeChunkCellWidget( MazeChunkWidget parent, MazeChunkData data, int row, int col )
		: base( parent )
	{
		Data = data;
		Row = row;
		Col = col;

		FixedSize = new Vector2( 32f, 32f );

		ToolTip = $"{Row + 1}, {Col + 1}";
	}

	private Direction? GetDirection( Vector2 localPos )
	{
		var pos = (localPos / Size - 0.5f) * 2f;

		if ( MathF.Abs( pos.x ) < 0.5f && MathF.Abs( pos.y ) < 0.5f )
		{
			return null;
		}

		if ( MathF.Abs( pos.x ) > MathF.Abs( pos.y ) )
		{
			return pos.x <= 0f ? Direction.West : Direction.East;
		}

		return pos.y <= 0f ? Direction.North : Direction.South;
	}

	protected override void OnMouseClick( MouseEvent e )
	{
		base.OnMouseClick( e );

		if ( GetDirection( e.LocalPosition ) is { } dir )
		{
			var prev = Data[Row, Col, dir];
			var next = prev == WallState.Closed ? WallState.Open : WallState.Closed;

			Data[Row, Col, dir] = next;
			Update();

			SignalValuesChanged();
		}
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = true;

		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlText.Darken( 0.8f ) );

		Paint.DrawRect( ContentRect.Shrink( 1f ) );

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
	}
}
