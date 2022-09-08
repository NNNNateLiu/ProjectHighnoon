using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyView : View
{
	[SerializeField]
	private Button toggleReadyButton;

	[SerializeField]
	private TextMeshProUGUI toggleReadyButtonText;

	[SerializeField]
	private Button startGameButton;

	public override void Initialize()
	{
		toggleReadyButton.onClick.AddListener(() => 
			Player.Instance.ServerSetIsReady(!Player.Instance.isReady)
			);
		
		startGameButton.onClick.AddListener(() =>
			GameManager.Instance.StartGame()
		);

		Debug.Log("start button work");

		//startGameButton.gameObject.SetActive(true);

		if (InstanceFinder.IsHost)
		{
			
		}
		else
		{
			//startGameButton.gameObject.SetActive(false);
		}

		base.Initialize();
	}

	private void Update()
	{
		if (!Initialized) return;

		toggleReadyButtonText.color = Player.Instance.isReady ? Color.green : Color.red;

		startGameButton.interactable = GameManager.Instance.canStart;
	}
}
