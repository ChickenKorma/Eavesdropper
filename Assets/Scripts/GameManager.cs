using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	#region Variables

	public static GameManager Instance { get; private set; }

	public InputAction m_topHitAction;
	public InputAction m_bottomHitAction;

	[SerializeField] private GameObject m_casePrefab;
	[SerializeField] private GameObject m_wordBoxPrefab;

	[SerializeField] private GameObject m_endScreen;

	public float BoxMoveSpeed { get; private set; }
	[SerializeField] private float m_boxStartingSpeed;
	[SerializeField] private float m_boxAccelerationFactor;

	[SerializeField] private float m_boxStartingSendTime;
	[SerializeField] private float m_boxSendTimeDecreaseFactor;
	private float m_boxSendTime;

	[SerializeField] private TMP_Text m_healthText;
	[SerializeField] private TMP_Text m_casesDoneText;

	[SerializeField] private AudioSource m_gameEndAudioSource;
	[SerializeField] private AudioSource m_wordSuccessAudioSource;
	[SerializeField] private AudioSource m_wordFailureAudioSource;
	[SerializeField] private AudioSource m_caseDoneAudioSource;
	[SerializeField] private AudioSource m_musicAudioSource;

	private int m_health = 5;
	private float m_lastDamageTime = 0;

	private const string s_caseContentResourceName = "caseContent";

	public string[] m_caseContentLines;
	private int m_caseContentLinesIndex = 0;
	private int m_casesDone = -1; // Starts at -1 so the initial NewCase() puts this to 0
	private float m_caseStartTime = 0;

	List<string> m_stationaryWords;
	List<WordBox> m_stationaryWordBoxes;
	List<WordBox> m_movingWordBoxes;

	private float m_boxBias = 1;

	#endregion

	#region Unity

	private void Awake()
	{
		if (Instance != null)
			Destroy(this);
		else
			Instance = this;
	}

	private void Start()
	{
		TextAsset textAsset = Resources.Load<TextAsset>(s_caseContentResourceName);
		//m_caseContentLines = textAsset.text.Split('\n');
		m_caseContentLines = RandomizeArray(textAsset.text.Split('\n'));

		m_endScreen.SetActive(false);

		BoxMoveSpeed = m_boxStartingSpeed;
		m_boxSendTime = m_boxStartingSendTime;

		UpdateHealthText();

		NewCase();
	}

	private void OnEnable()
	{
		m_topHitAction.Enable();
		m_bottomHitAction.Enable();

		m_topHitAction.performed += OnTopHit;
		m_bottomHitAction.performed += OnBottomHit;
	}

	private void OnDisable()
	{
		m_topHitAction.Disable();
		m_bottomHitAction.Disable();

		m_topHitAction.performed -= OnTopHit;
		m_bottomHitAction.performed -= OnBottomHit;
	}

	#endregion

	#region Inputs

	private void OnTopHit(InputAction.CallbackContext context) => CheckForBoxHit(true);

	private void OnBottomHit(InputAction.CallbackContext context) => CheckForBoxHit(false);

	private void CheckForBoxHit(bool checkTopBoxes)
	{
		bool hitSomething = false;

		if (m_movingWordBoxes != null)
			for (int i = 0; i < m_movingWordBoxes.Count; i++)
				if (m_movingWordBoxes[i] != null)
					if (m_movingWordBoxes[i].OnTarget(checkTopBoxes))
						hitSomething = true;

		if (!hitSomething)
			TakeDamage();
	}

	#endregion

	#region Case/Words handling

	public void NewCase()
	{
		m_casesDone++;

		BoxMoveSpeed += m_boxAccelerationFactor * (Time.time - m_caseStartTime);
		m_boxSendTime = Mathf.Clamp(m_boxSendTime - ((Time.time - m_caseStartTime) * m_boxSendTimeDecreaseFactor), 0.3f, m_boxStartingSendTime);
		m_musicAudioSource.pitch = Mathf.Clamp((0.1f * (BoxMoveSpeed - m_boxStartingSpeed) + m_boxStartingSpeed) / m_boxStartingSpeed, 1f, 3f);

		m_caseStartTime = Time.time;

		string caseContent = m_caseContentLines[m_caseContentLinesIndex];
		m_caseContentLinesIndex++;

		if (m_caseContentLinesIndex >= m_caseContentLines.Length)
			m_caseContentLinesIndex = 0;

		string trimmedCaseContent = caseContent.TrimEnd('\r');
		string[] caseWords = trimmedCaseContent.Split(' ');

		Instantiate(m_casePrefab).GetComponent<Case>().Setup(caseWords, m_casesDone + 1);

		m_movingWordBoxes = new List<WordBox>();
		m_stationaryWordBoxes = new List<WordBox>();
		m_stationaryWords = caseWords.ToList();

		for (int i = 0; i < caseWords.Length; i++)
		{
			m_stationaryWordBoxes.Add(Instantiate(m_wordBoxPrefab).GetComponent<WordBox>());
		}

		StartCoroutine(SendBoxes());
	}

	private IEnumerator SendBoxes()
	{
		while (m_stationaryWordBoxes.Count > 0)
		{
			bool isTopTrack = UnityEngine.Random.value > 0.5f * m_boxBias;
			m_boxBias += isTopTrack ? 0.1f : -0.1f;

			m_stationaryWordBoxes[0].Setup(isTopTrack, m_stationaryWords[0]);

			m_movingWordBoxes.Add(m_stationaryWordBoxes[0]);

			m_stationaryWords.RemoveAt(0);
			m_stationaryWordBoxes.RemoveAt(0);

			yield return new WaitForSeconds(UnityEngine.Random.Range(m_boxSendTime - 0.05f, m_boxSendTime + 0.05f));
		}
	}

	#endregion

	#region Game systems handling

	public void TakeDamage()
	{
		if (Time.time > m_lastDamageTime + 0.1f)
		{
			m_lastDamageTime = Time.time;

			m_health--;

			PlayWordFailureSound();

			UpdateHealthText();
			m_healthText.transform.DOShakePosition(0.5f, 2.5f);

			if (m_health <= 0)
			{
				m_endScreen.SetActive(true);
				m_casesDoneText.text = $"You solved {m_casesDone} cases";
				PlayGameEndSound();
				m_musicAudioSource.Stop();

				Time.timeScale = 0;
			}
		}
	}

	private void UpdateHealthText() => m_healthText.text = $"Lives: {m_health}";

	public void RestartGame()
	{
		Time.timeScale = 1;
		SceneManager.LoadScene(1); // Reload this scene
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	#endregion

	#region Audio

	public void PlayWordSuccessSound() => m_wordSuccessAudioSource.Play();

	public void PlayWordFailureSound() => m_wordFailureAudioSource.Play();

	public void PlayGameEndSound() => m_gameEndAudioSource.Play();

	public void PlayCaseDoneSound() => m_caseDoneAudioSource.Play();

	#endregion

	#region Helper methods

	public static string[] RandomizeArray(string[] array) =>
	array.OrderBy(x => Guid.NewGuid()).ToArray();

	#endregion
}
