using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Case : MonoBehaviour
{
	#region Variables

	public static Case Instance { get; private set; }

	[SerializeField] private GameObject m_wordObjectPrefab;

	private string[] m_words;
	private TMP_Text[] m_wordObjects;

	#endregion

	#region Unity

	private void Awake()
	{
		if (Instance != null)
			Destroy(gameObject);
		else
			Instance = this;
	}

	#endregion

	#region Setup

	public void Setup(string[] words, int caseNumber)
	{
		m_words = words;
		m_wordObjects = new TMP_Text[m_words.Length];

		Transform panelTransform = transform.GetChild(0).GetChild(1);

		for (int i = 0; i < m_words.Length; i++)
		{
			GameObject caseWord = Instantiate(m_wordObjectPrefab, panelTransform);

			m_wordObjects[i] = caseWord.GetComponent<TMP_Text>();

			string blankText = "";

			foreach (char c in m_words[i])
			{
				blankText += "-";
			}

			m_wordObjects[i].text = blankText;
		}

		transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = $"Case #{caseNumber}";

		StartCoroutine(ApplyLayout());
	}

	private IEnumerator ApplyLayout()
	{
		yield return new WaitForEndOfFrame();

		FlowLayoutGroup flowLayoutGroup = transform.GetChild(0).GetChild(1).GetComponent<FlowLayoutGroup>();
		flowLayoutGroup.SetLayoutHorizontal();
	}

	#endregion

	#region Logic

	public void WordDone(string word, bool success)
	{
		int wordIndex = Array.LastIndexOf(m_words, word);

		if (wordIndex == -1)
			return;

		if (success)
		{
			DOTweenTMPAnimator animator = new(m_wordObjects[wordIndex]);
			Sequence sequence = DOTween.Sequence();

			for (int i = 0; i < animator.textInfo.characterCount; i++)
			{
				if (!animator.textInfo.characterInfo[i].isVisible) continue;

				sequence.Append(animator.DOPunchCharScale(i, 0.5f, 0.06f));
			}

			m_wordObjects[wordIndex].DOText(word, word.Length * 0.06f);
		}

		if (wordIndex == m_words.Length - 1)
			StartCoroutine(EndCase());
	}

	private IEnumerator EndCase()
	{
		Instance = null;

		GameManager.Instance.PlayCaseDoneSound();
		transform.GetChild(0).GetComponent<RectTransform>().DOPunchScale(new(0.05f, 0.05f), 0.2f);

		yield return new WaitForSeconds(1f);

		gameObject.SetActive(false);
		Destroy(gameObject);

		GameManager.Instance.NewCase();
	}

	#endregion
}
