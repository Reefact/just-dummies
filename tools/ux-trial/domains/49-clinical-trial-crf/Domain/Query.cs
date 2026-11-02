using System.Text.RegularExpressions;

namespace ClinicalTrial.Domain;

/// <summary>
/// A data query raised by a monitor on one field of a form. Open until the site answers it, then closed by the
/// monitor once the answer is accepted.
/// </summary>
public sealed partial class Query
{
    public const int MaxTextLength = 500;

    public Query(int number, string fieldName, string text, DateOnly raisedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentNullException.ThrowIfNull(fieldName);
        if (!FieldPattern().IsMatch(fieldName))
        {
            throw new FormatException($"'{fieldName}' is not a field path (PascalCase names separated by dots, e.g. Vitals.HeartRateBpm).");
        }

        CheckText(text, nameof(text));
        Number = number;
        FieldName = fieldName;
        Text = text;
        RaisedOn = raisedOn;
        Status = QueryStatus.Open;
    }

    public int Number { get; }

    public string FieldName { get; }

    public string Text { get; }

    public DateOnly RaisedOn { get; }

    public QueryStatus Status { get; private set; }

    public string? Response { get; private set; }

    public DateOnly? AnsweredOn { get; private set; }

    public DateOnly? ClosedOn { get; private set; }

    internal void Answer(string response, DateOnly answeredOn)
    {
        CheckText(response, nameof(response));
        if (answeredOn < RaisedOn)
        {
            throw new ArgumentOutOfRangeException(nameof(answeredOn), answeredOn, $"Query {Number} cannot be answered before it was raised ({RaisedOn:yyyy-MM-dd}).");
        }

        Response = response;
        AnsweredOn = answeredOn;
        Status = QueryStatus.Answered;
    }

    internal void Close(DateOnly closedOn)
    {
        if (closedOn < AnsweredOn)
        {
            throw new ArgumentOutOfRangeException(nameof(closedOn), closedOn, $"Query {Number} cannot be closed before it was answered ({AnsweredOn:yyyy-MM-dd}).");
        }

        ClosedOn = closedOn;
        Status = QueryStatus.Closed;
    }

    private static void CheckText(string text, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, parameterName);
        if (text.Length > MaxTextLength)
        {
            throw new ArgumentException($"Query text has at most {MaxTextLength} characters.", parameterName);
        }
    }

    [GeneratedRegex(@"^[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)*$")]
    private static partial Regex FieldPattern();
}
