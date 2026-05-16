namespace Tests.E2E.TestData;

public sealed record TenderScenario(
    Guid TenderId,
    Guid SubmissionId,
    TenderDraft Tender,
    TenderScenarioQuestions Questions,
    TenderScenarioAnswers Answers,
    string ReviewerName,
    string SupplierName);

public sealed record TenderScenarioQuestions(
    Guid TextQuestionId,
    string TextQuestion,
    Guid NumericQuestionId,
    string NumericQuestion,
    Guid ChoiceQuestionId,
    string ChoiceQuestion,
    Guid ChoiceOptionId,
    string ChoiceOption);

public sealed record TenderScenarioAnswers(
    string TextAnswer,
    string NumericAnswer,
    string ChoiceAnswer);
