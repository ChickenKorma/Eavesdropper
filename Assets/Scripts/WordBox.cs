using System.Collections;
using TMPro;
using UnityEngine;

public class WordBox : MonoBehaviour
{
	#region Variables

	private RectTransform m_wordRectTransform;

	private bool m_isTopTrack;

	private Vector2 m_moveDirection;
	private bool m_moving;

	private float m_endPoint;
	private float m_boxCenterRange;

	private TMP_Text m_boxText;
	private TMP_Text m_boxSecondText; // Two text objects, first influences the box size and the second is copied on top to show word
	private string m_word;

	private bool m_failed;

	#endregion

	#region Unity

	private void Awake()
	{
		m_wordRectTransform = transform.GetChild(0).GetComponent<RectTransform>();

		m_boxText = transform.GetChild(0).GetComponentInChildren<TMP_Text>();
		m_boxSecondText = transform.GetChild(0).GetChild(0).GetChild(0).GetComponentInChildren<TMP_Text>();
	}

	private void Update()
	{
		if (m_moving)
		{
			m_wordRectTransform.anchoredPosition += GameManager.Instance.BoxMoveSpeed * Time.deltaTime * m_moveDirection;

			float xPos = m_wordRectTransform.anchoredPosition.x;

			if (!m_failed && IsPastMiddle(xPos))
			{
				Case.Instance.WordDone(m_word, false);
				GameManager.Instance.TakeDamage();
				GameManager.Instance.PlayWordFailureSound();
				m_failed = true;
			}
			else if (m_failed && IsAtEnd(xPos))
			{
				Destroy(gameObject);
			}
		}
	}

	#endregion

	#region Setup

	public void Setup(bool isTopTrack, string word)
	{
		m_isTopTrack = isTopTrack;
		m_wordRectTransform.anchoredPosition = isTopTrack ? new Vector2(-620, -45) : new Vector2(620, -165);
		m_moveDirection = new Vector2(isTopTrack ? 1 : -1, 0);

		m_word = word;
		m_boxText.text = word;
		m_boxSecondText.text = word;

		StartCoroutine(UpdateSize());
	}

	private IEnumerator UpdateSize()
	{
		yield return new WaitForEndOfFrame();

		float boxWidth = m_wordRectTransform.rect.width;
		m_endPoint = m_isTopTrack ? (boxWidth / 2f) + 20 : (-boxWidth / 2f) - 20;
		m_boxCenterRange = (boxWidth / 2f) + 10;

		m_moving = true;
	}

	#endregion

	#region Position checks

	private bool IsPastMiddle(float xPos)
	{
		if (m_isTopTrack)
			return xPos > m_endPoint;
		else
			return xPos < m_endPoint;
	}

	private bool IsAtEnd(float xPos)
	{
		if (m_isTopTrack)
			return xPos > 330;
		else
			return xPos < -330;
	}

	public bool OnTarget(bool checkTopTrack)
	{
		if (checkTopTrack == m_isTopTrack && Mathf.Abs(m_wordRectTransform.anchoredPosition.x) <= m_boxCenterRange)
		{
			Case.Instance.WordDone(m_word, true);
			GameManager.Instance.PlayWordSuccessSound();
			Destroy(gameObject);
			return true;
		}

		return false;
	}

	#endregion
}
