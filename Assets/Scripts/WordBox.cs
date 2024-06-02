using System.Collections;
using TMPro;
using UnityEngine;

public class WordBox : MonoBehaviour
{
	private RectTransform m_wordRectTransform;

	private Vector2 m_moveDirection;

	private float m_boxWidth;

	private bool m_isGoingRight;

	private float m_endPoint;

	private TMP_Text m_boxText;
	private TMP_Text m_boxSecondText;

	private bool m_moving;

	private string m_word;

	private bool m_failed;

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

	private bool IsPastMiddle(float xPos)
	{
		if (m_isGoingRight)
			return xPos > m_endPoint;
		else
			return xPos < m_endPoint;
	}

	private bool IsAtEnd(float xPos)
	{
		if (m_isGoingRight)
			return xPos > 330;
		else
			return xPos < -330;
	}

	public void Setup(bool goingRight, string word)
	{
		m_wordRectTransform.anchoredPosition = goingRight ? new Vector2(-620, -45) : new Vector2(620, -165);

		m_isGoingRight = goingRight;

		m_moveDirection = new Vector2(m_isGoingRight ? 1 : -1, 0);

		m_boxText.text = word;
		m_boxSecondText.text = word;

		m_word = word;

		m_boxWidth = 1.6f;

		StartCoroutine(UpdateSize());
	}

	public bool OnTarget(bool lookingForTopBox)
	{
		if (lookingForTopBox == m_isGoingRight && Mathf.Abs(m_wordRectTransform.anchoredPosition.x) <= (m_boxWidth / 2f) + 10)
		{
			Case.Instance.WordDone(m_word, true);
			GameManager.Instance.PlayWordSuccessSound();
			Destroy(gameObject);
			return true;
		}

		return false;
	}

	private IEnumerator UpdateSize()
	{
		yield return new WaitForEndOfFrame();

		m_boxWidth = m_wordRectTransform.rect.width;
		m_endPoint = m_isGoingRight ? (m_boxWidth / 2f) + 20 : (-m_boxWidth / 2f) - 20;

		m_moving = true;
	}
}
