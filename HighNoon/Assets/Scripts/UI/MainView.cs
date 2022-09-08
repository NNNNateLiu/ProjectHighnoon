using TMPro;
using UnityEngine;

public sealed class MainView : View
{
	[SerializeField]
	private TextMeshProUGUI healthText;
	
	[SerializeField]
	private TextMeshProUGUI roundTimeText;

	private void Update()
	{
		if (!Initialized) return;

		Player player = Player.Instance;

		if (player == null || player.controlledPawn == null) return;

		healthText.text = $"Health {player.controlledPawn.health}";

		roundTimeText.text = $"Round Time {Mathf.Round(GameManager.Instance.roundTotalTime)}";
	}
}
