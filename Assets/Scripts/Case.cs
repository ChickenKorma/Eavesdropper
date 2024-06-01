using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Case : MonoBehaviour
{
	public static Case Instance { get; private set; }

	private string[] m_words;

	private TMP_Text[] m_wordObjects;

	[SerializeField] private GameObject m_wordObjectPrefab;

	private void Awake()
	{
		if (Instance != null)
			Destroy(gameObject);
		else
			Instance = this;
	}

	public void Setup(string[] words)
	{
		m_words = words;
		m_wordObjects = new TMP_Text[m_words.Length];

		for (int i = 0; i < m_words.Length; i++)
		{
			GameObject caseWord = Instantiate(m_wordObjectPrefab, transform);

			m_wordObjects[i] = caseWord.GetComponent<TMP_Text>();
		}

		StartCoroutine(ApplyLayout());
	}

	private IEnumerator ApplyLayout()
	{
		yield return new WaitForEndOfFrame();

		string text = m_wordObjects[0].text;
		m_wordObjects[0].text = "awfaw";
		m_wordObjects[0].text = text;
	}

	public void WordDone(string word, bool success)
	{
		int wordIndex = Array.IndexOf(m_words, word);

		if (wordIndex == -1)
			return;

		if (success)
			m_wordObjects[wordIndex].text = m_words[wordIndex];

		if (wordIndex == m_words.Length - 1)
			StartCoroutine(EndCase());
	}

	private IEnumerator EndCase()
	{
		Instance = null;

		GameManager.Instance.PlayCaseDoneSound();

		yield return new WaitForSeconds(1f);

		Destroy(gameObject);
		GameManager.Instance.StartCoroutine(GameManager.Instance.NewCase());
	}
}
