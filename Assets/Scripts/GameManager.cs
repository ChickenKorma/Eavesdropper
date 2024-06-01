using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	public InputAction m_topHitAction;
	public InputAction m_bottomHitAction;

	public float BoxMoveSpeed { get; private set; }

	[SerializeField] private GameObject m_wordObjectPrefab;

	private float m_boxSendTime;

	[SerializeField] private GameObject m_caseObjectPrefab;

	[SerializeField] private GameObject m_endScreen;

	[SerializeField] private float m_boxStartingSpeed;
	[SerializeField] private float m_boxAccelerationFactor;
	[SerializeField] private float m_boxStartingSendTime;
	[SerializeField] private float m_boxSendTimeDecreaseFactor;

	[SerializeField] private string m_caseContentFilePath;

	[SerializeField] private TMP_Text m_healthText;
	[SerializeField] private TMP_Text m_caseText;
	[SerializeField] private TMP_Text m_casesDoneText;

	private StreamReader m_streamReader;

	private int m_health;
	private int m_casesDone;

	List<WordBox> m_stationaryWordBoxes;
	List<string> m_stationaryWords;

	List<WordBox> m_movingBoxes;

	private void Awake()
	{
		if (Instance != null)
			Destroy(this);
		else
			Instance = this;
	}

	private void Start()
	{
		string path = Application.streamingAssetsPath + m_caseContentFilePath;
		m_streamReader = new(path);

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
		m_boxSendTime = Mathf.Clamp(m_boxSendTime - (m_boxSendTimeDecreaseFactor * Time.deltaTime), 0.1f, m_boxStartingSendTime);
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

	private void OnApplicationQuit()
	{
		m_streamReader?.Dispose();
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

		string caseContent = m_streamReader.ReadLine();
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
			bool sendRight = Random.Range(0, 1) == 1;

			m_stationaryWordBoxes[0].Setup(sendRight, m_stationaryWords[0]);

			m_movingBoxes.Add(m_stationaryWordBoxes[0]);

			m_stationaryWords.RemoveAt(0);
			m_stationaryWordBoxes.RemoveAt(0);

			yield return new WaitForSeconds(Random.Range(m_boxSendTime - 0.15f, m_boxSendTime + 0.15f));
		}
	}

	public void TakeDamage()
	{
		m_health -= 1;

		UpdateHealthText();

		if (m_health <= 0)
		{
			m_endScreen.SetActive(true);
			m_casesDoneText.text = $"You solved {m_casesDone} cases";
		}
	}

	private void UpdateHealthText() => m_healthText.text = $"Health: {m_health}";

	private void UpdateCaseText() => m_caseText.text = $"Case #{m_casesDone + 1}";

	public void RestartGame()
	{
		m_streamReader.Dispose();

		string currentSceneName = SceneManager.GetActiveScene().name;
		SceneManager.LoadScene(currentSceneName);
	}
}
