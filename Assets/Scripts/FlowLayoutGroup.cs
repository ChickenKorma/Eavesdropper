// Slightly updated the answer from https://stackoverflow.com/questions/38336835/correct-flowlayoutgroup-in-unity3d-as-per-horizontallayoutgroup-etc

using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Flow Layout Group", 153)]
public class FlowLayoutGroup : LayoutGroup
{
	#region Variables

	public Vector2 CellSize
	{
		get => m_CellSize;
		set => SetProperty(ref m_CellSize, value);
	}

	public Vector2 Spacing
	{
		get => m_Spacing;
		set => SetProperty(ref m_Spacing, value);
	}

	private Vector2 m_CellSize = new(100, 100);

	[SerializeField] private Vector2 m_Spacing = Vector2.zero;

	private float m_lastMaxHeight = 0;

	#endregion

	#region Enums

	public enum Corner
	{
		UpperLeft = 0,
		UpperRight = 1,
		LowerLeft = 2,
		LowerRight = 3
	}

	public enum Constraint
	{
		Flexible = 0,
		FixedColumnCount = 1,
		FixedRowCount = 2
	}

	#endregion

	#region Unity

	protected FlowLayoutGroup()
	{ }

#if UNITY_EDITOR
	protected override void OnValidate()
	{
		base.OnValidate();
	}

#endif

	#endregion

	#region Overrides

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();

		int preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count));

		SetLayoutInputForAxis(
			padding.horizontal + (CellSize.x + Spacing.x) - Spacing.x,
			padding.horizontal + (CellSize.x + Spacing.x) * preferredColumns - Spacing.x,
			-1, 0);
	}

	public override void CalculateLayoutInputVertical()
	{
		float minSpace = padding.vertical + (CellSize.y + Spacing.y) - Spacing.y;
		SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
	}

	public override void SetLayoutHorizontal() => SetCellsAlongAxis();

	public override void SetLayoutVertical() => SetCellsAlongAxis();

	#endregion

	#region Logic

	private void SetCellsAlongAxis()
	{
		// Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
		// and only vertical values when invoked for the vertical axis.
		// However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
		// Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
		// and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.

		float width = rectTransform.rect.size.x;
		float height = rectTransform.rect.size.y;

		int cellCountX;
		int cellCountY;

		if (CellSize.x + Spacing.x <= 0)
			cellCountX = int.MaxValue;
		else
			cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + Spacing.x + 0.001f) / (CellSize.x + Spacing.x)));

		if (CellSize.y + Spacing.y <= 0)
			cellCountY = int.MaxValue;
		else
			cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + Spacing.y + 0.001f) / (CellSize.y + Spacing.y)));

		int actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildren.Count);
		int actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildren.Count / cellCountX));

		Vector2 requiredSpace = new(
			actualCellCountX * CellSize.x + (actualCellCountX - 1) * Spacing.x,
			actualCellCountY * CellSize.y + (actualCellCountY - 1) * Spacing.y
		);

		Vector2 startOffset = new(
			GetStartOffset(0, requiredSpace.x),
			GetStartOffset(1, requiredSpace.y)
		);

		float totalWidth = 0;
		float totalHeight = 0;
		Vector2 currentSpacing;

		for (int i = 0; i < rectChildren.Count; i++)
		{
			SetChildAlongAxis(rectChildren[i], 0, startOffset.x + totalWidth /*+ currentSpacing[0]*/, rectChildren[i].rect.size.x);
			SetChildAlongAxis(rectChildren[i], 1, startOffset.y + totalHeight  /*+ currentSpacing[1]*/, rectChildren[i].rect.size.y);

			currentSpacing = Spacing;

			totalWidth += rectChildren[i].rect.width + currentSpacing[0];

			if (rectChildren[i].rect.height > m_lastMaxHeight)
			{
				m_lastMaxHeight = rectChildren[i].rect.height;
			}

			if (i < rectChildren.Count - 1)
			{
				if (totalWidth + rectChildren[i + 1].rect.width + currentSpacing[0] > width)
				{
					totalWidth = 0;
					totalHeight += m_lastMaxHeight + currentSpacing[1];
					m_lastMaxHeight = 0;
				}
			}
		}
	}

	#endregion
}