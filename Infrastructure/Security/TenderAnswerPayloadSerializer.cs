using Domain.Entities.TenderAnswers;
using Domain.Enums;
using System.Text.Json;

namespace Infrastructure.Security;

public class TenderAnswerPayloadSerializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public byte[] Serialize(TenderAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        return answer switch
        {
            TextAnswer textAnswer => JsonSerializer.SerializeToUtf8Bytes(
                new TextAnswerPayload(textAnswer.TextValue),
                JsonSerializerOptions),
            NumberAnswer numberAnswer => JsonSerializer.SerializeToUtf8Bytes(
                new NumberAnswerPayload(numberAnswer.NumericValue),
                JsonSerializerOptions),
            ChoiceAnswer choiceAnswer => JsonSerializer.SerializeToUtf8Bytes(
                new ChoiceAnswerPayload(
                    [.. choiceAnswer.Selections.Select(selection => selection.OptionId).Distinct()]),
                JsonSerializerOptions),
            _ => throw new InvalidOperationException("Het opgegeven antwoordtype kan niet worden versleuteld.")
        };
    }

    public void Populate(TenderAnswer answer, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(payload);

        switch (answer)
        {
            case TextAnswer textAnswer:
                var textPayload = JsonSerializer.Deserialize<TextAnswerPayload>(payload, JsonSerializerOptions)
                    ?? throw new InvalidOperationException("De opgeslagen tekstpayload kon niet worden gelezen.");
                textAnswer.TextValue = textPayload.TextValue;
                break;

            case NumberAnswer numberAnswer:
                var numberPayload = JsonSerializer.Deserialize<NumberAnswerPayload>(payload, JsonSerializerOptions)
                    ?? throw new InvalidOperationException("De opgeslagen numerieke payload kon niet worden gelezen.");
                numberAnswer.NumericValue = numberPayload.NumericValue;
                break;

            case ChoiceAnswer choiceAnswer:
                var choicePayload = JsonSerializer.Deserialize<ChoiceAnswerPayload>(payload, JsonSerializerOptions)
                    ?? throw new InvalidOperationException("De opgeslagen keuze-payload kon niet worden gelezen.");

                choiceAnswer.Selections = choicePayload.SelectedOptionIds
                    .Distinct()
                    .Select(optionId => new ChoiceAnswerSelection
                    {
                        ChoiceAnswerId = choiceAnswer.Id,
                        OptionId = optionId
                    })
                    .ToList();
                break;

            default:
                throw new InvalidOperationException("Het opgegeven antwoordtype kan niet worden ontsleuteld.");
        }
    }

    private sealed record TextAnswerPayload(string? TextValue);

    private sealed record NumberAnswerPayload(decimal? NumericValue);

    private sealed record ChoiceAnswerPayload(List<Guid> SelectedOptionIds);
}
