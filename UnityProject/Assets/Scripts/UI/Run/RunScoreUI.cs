using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Game Over score card: one page per player (own kills) over the team's points per source and the total sent to the leaderboard.
public class RunScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text labelsText;
    [SerializeField] private TMP_Text valuesText;
    [SerializeField] private string totalColor = "#FFD35A";
    [SerializeField] private string goldColor = "#F2B233";

    private readonly StringBuilder labels = new StringBuilder();
    private readonly StringBuilder values = new StringBuilder();
    private RunResult result;
    private PlayerKillTable players;
    private int selected;
    private string goldDisplay = "...";

    void Awake()
    {
        previousButton.onClick.AddListener(() => Step(-1));
        nextButton.onClick.AddListener(() => Step(1));
    }

    // Opens on the local player's page; the gold line waits for the server's answer.
    public void Show(RunResult runResult, PlayerKillTable killTable, ulong localClientId)
    {
        result = runResult;
        goldDisplay = GoldWallet.CanEarn ? "..." : "Sign in";
        players = killTable;
        selected = Mathf.Max(0, players.IndexOf(localClientId));

        bool multi = players.count > 1;
        previousButton.gameObject.SetActive(multi);
        nextButton.gameObject.SetActive(multi);
        Render();
    }

    // Gold comes from the team total, so it is the same on every player's page.
    public void SetGoldResult(int gold)
    {
        goldDisplay = gold == GoldWallet.Offline ? "Sign in" : gold == GoldWallet.Refused ? "Refused" : $"+{gold}";
        Render();
    }

    private void Step(int direction)
    {
        if (players.count <= 1)
            return;

        selected = (selected + direction + players.count) % players.count;
        Render();
    }

    // Kills are the selected player's own; waves, boss and the total are shared by the team.
    private void Render()
    {
        ScoreBreakdown score = result.score;
        bool multi = players.count > 1;
        PlayerKillScore player = players.count > 0 ? players[selected] : default;
        nameText.text = player.name.ToString();

        labels.Clear();
        values.Clear();
        if (multi)
            AddLine($"Kills ({player.kills})", player.killPoints);
        else
            AddLine($"Kills ({score.kills})", score.killPoints);
        AddLine($"Waves ({result.wave})", score.wavePoints);
        if (score.bossPoints > 0)
            AddLine("Boss", score.bossPoints);

        labels.Append($"\n<color={totalColor}>Total</color>");
        values.Append($"\n<color={totalColor}>{score.Total}</color>");
        labels.Append($"\n<color={goldColor}>Gold</color>");
        values.Append($"\n<color={goldColor}>{goldDisplay}</color>");

        labelsText.text = labels.ToString();
        valuesText.text = values.ToString();
    }

    private void AddLine(string label, int points)
    {
        labels.Append(label).Append('\n');
        values.Append(points).Append('\n');
    }
}
