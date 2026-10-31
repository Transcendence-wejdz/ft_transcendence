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
    private const string PendingGold = "...";
    private const string SignInGold = "Sign in";
    private const string RefusedGold = "Refused";

    private string goldDisplay = PendingGold;

    private static string GoldText(int gold)
    {
        if (gold == GoldWallet.Offline)
            return SignInGold;
        if (gold == GoldWallet.Refused)
            return RefusedGold;
        return $"+{gold}";
    }

    private void Step(int direction)
    {
        if (players.count <= 1)
            return;
        selected = (selected + direction + players.count) % players.count;
        Render();
    }

    private void StepPrevious()
    {
        Step(-1);
    }

    private void StepNext()
    {
        Step(1);
    }

    private void AddLine(string label, int points)
    {
        labels.Append(label).Append('\n');
        values.Append(points).Append('\n');
    }

    private void AddColoredLine(string label, string value, string color)
    {
        labels.Append($"\n<color={color}>{label}</color>");
        values.Append($"\n<color={color}>{value}</color>");
    }

    private PlayerKillScore SelectedPlayer()
    {
        if (players.count == 0)
            return default;
        return players[selected];
    }

    // Kills are the selected player's own; waves, boss and the total are shared by the team.
    private void Render()
    {
        ScoreBreakdown score = result.score;
        PlayerKillScore player = SelectedPlayer();

        nameText.text = player.name.ToString();
        labels.Clear();
        values.Clear();
        if (players.count > 1)
            AddLine($"Kills ({player.kills})", player.killPoints);
        else
            AddLine($"Kills ({score.kills})", score.killPoints);
        AddLine($"Waves ({result.wave})", score.wavePoints);
        if (score.bossPoints > 0)
            AddLine("Boss", score.bossPoints);
        AddColoredLine("Total", score.Total.ToString(), totalColor);
        AddColoredLine("Gold", goldDisplay, goldColor);
        labelsText.text = labels.ToString();
        valuesText.text = values.ToString();
    }

    // Opens on the local player's page; the gold line waits for the server's answer.
    public void Show(RunResult runResult, PlayerKillTable killTable, ulong localClientId)
    {
        bool multi;

        result = runResult;
        goldDisplay = SignInGold;
        if (GoldWallet.CanEarn)
            goldDisplay = PendingGold;
        players = killTable;
        selected = Mathf.Max(0, players.IndexOf(localClientId));
        multi = players.count > 1;
        previousButton.gameObject.SetActive(multi);
        nextButton.gameObject.SetActive(multi);
        Render();
    }

    // Gold comes from the team total, so it is the same on every player's page.
    public void SetGoldResult(int gold)
    {
        goldDisplay = GoldText(gold);
        Render();
    }

    void Awake()
    {
        previousButton.onClick.AddListener(StepPrevious);
        nextButton.onClick.AddListener(StepNext);
    }
}
