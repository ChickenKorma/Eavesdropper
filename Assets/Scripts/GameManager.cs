using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	public string[] m_caseContentLines;
	private int m_caseContentLinesIndex = 0;

	public InputAction m_topHitAction;
	public InputAction m_bottomHitAction;

	private float m_lastDamageTime = 0;

	public float BoxMoveSpeed { get; private set; }

	[SerializeField] private GameObject m_wordObjectPrefab;

	private float m_boxSendTime;

	[SerializeField] private GameObject m_caseObjectPrefab;

	[SerializeField] private GameObject m_endScreen;

	[SerializeField] private float m_boxStartingSpeed;
	[SerializeField] private float m_boxAccelerationFactor;
	[SerializeField] private float m_boxStartingSendTime;
	[SerializeField] private float m_boxSendTimeDecreaseFactor;

	[SerializeField] private TMP_Text m_healthText;
	[SerializeField] private TMP_Text m_caseText;
	[SerializeField] private TMP_Text m_casesDoneText;

	[SerializeField] private AudioSource m_gameEndAudioSource;
	[SerializeField] private AudioSource m_wordSuccessAudioSource;
	[SerializeField] private AudioSource m_wordFailureAudioSource;
	[SerializeField] private AudioSource m_caseDoneAudioSource;

	private int m_health;
	private int m_casesDone;

	List<WordBox> m_stationaryWordBoxes;
	List<string> m_stationaryWords;

	List<WordBox> m_movingBoxes;

	private float m_boxBias = 1;

	private void Awake()
	{
		if (Instance != null)
			Destroy(this);
		else
			Instance = this;
	}

	private void Start()
	{
		TextAsset textAsset = Resources.Load<TextAsset>("caseContent");
		m_caseContentLines = textAsset.text.Split('\n');

		m_endScreen.SetActive(false);
		BoxMoveSpeed = m_boxStartingSpeed;
		m_boxSendTime = m_boxStartingSendTime;

		m_casesDone = -1;
		m_health = 3;

		UpdateHealthText();
		UpdateCaseText();

		StartCoroutine(NewCase());
	}

	void Update()
	{
		BoxMoveSpeed += m_boxAccelerationFactor * Time.deltaTime;
		m_boxSendTime = Mathf.Clamp(m_boxSendTime - (m_boxSendTimeDecreaseFactor * Time.deltaTime), 0.4f, m_boxStartingSendTime);
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

	private void OnTopHit(InputAction.CallbackContext context)
	{
		CheckForBoxHit(true);
	}

	private void OnBottomHit(InputAction.CallbackContext context)
	{
		CheckForBoxHit(false);
	}

	private void CheckForBoxHit(bool checkingTopBoxes)
	{
		bool hitSomething = false;

		if (m_movingBoxes != null)
		{
			for (int i = 0; i < m_movingBoxes.Count; i++)
			{
				if (m_movingBoxes[i] != null)
					if (m_movingBoxes[i].OnTarget(checkingTopBoxes))
						hitSomething = true;
			}
		}

		if (!hitSomething)
			TakeDamage();
	}

	public IEnumerator NewCase()
	{
		m_casesDone++;
		UpdateCaseText();

		yield return new WaitForEndOfFrame();

		string caseContent = m_caseContentLines[m_caseContentLinesIndex];
		m_caseContentLinesIndex++;

		if (m_caseContentLinesIndex >= m_caseContentLines.Length)
			m_caseContentLinesIndex = 0;

		string[] caseWords = caseContent.Split(' ');

		Instantiate(m_caseObjectPrefab).GetComponent<Case>().Setup(caseWords);

		m_movingBoxes = new List<WordBox>();
		m_stationaryWordBoxes = new List<WordBox>();
		m_stationaryWords = caseWords.ToList();

		for (int i = 0; i < caseWords.Length; i++)
		{
			m_stationaryWordBoxes.Add(Instantiate(m_wordObjectPrefab).GetComponent<WordBox>());
		}

		StartCoroutine(SendBoxes());
	}

	private IEnumerator SendBoxes()
	{
		while (m_stationaryWordBoxes.Count > 0)
		{
			bool sendRight = Random.value > 0.5f * m_boxBias;
			m_boxBias += sendRight ? 0.1f : -0.1f;

			m_stationaryWordBoxes[0].Setup(sendRight, m_stationaryWords[0]);

			m_movingBoxes.Add(m_stationaryWordBoxes[0]);

			m_stationaryWords.RemoveAt(0);
			m_stationaryWordBoxes.RemoveAt(0);

			yield return new WaitForSeconds(Random.Range(m_boxSendTime - 0.15f, m_boxSendTime + 0.15f));
		}
	}

	public void TakeDamage()
	{
		if (Time.time > m_lastDamageTime + 0.1f)
		{
			m_lastDamageTime = Time.time;

			m_health -= 1;

			UpdateHealthText();

			if (m_health <= 0)
			{
				m_endScreen.SetActive(true);
				m_casesDoneText.text = $"You solved {m_casesDone} cases";
				PlayGameEndSound();

				Time.timeScale = 0;
			}
		}
	}

	private void UpdateHealthText() => m_healthText.text = $"Lives: {m_health}";

	private void UpdateCaseText() => m_caseText.text = $"Case #{m_casesDone + 1}";

	public void RestartGame()
	{
		Time.timeScale = 1;
		SceneManager.LoadScene(1);
	}

	public void PlayWordSuccessSound() => m_wordSuccessAudioSource.Play();

	public void PlayWordFailureSound() => m_wordFailureAudioSource.Play();

	public void PlayGameEndSound() => m_gameEndAudioSource.Play();

	public void PlayCaseDoneSound() => m_caseDoneAudioSource.Play();
}
