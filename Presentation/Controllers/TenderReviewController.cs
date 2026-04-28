using Application.Interfaces.Services;
using Application.Models.TenderReview;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.Builders;
using Presentation.Models.TenderReview;

namespace Presentation.Controllers;

[Route("Tender/{tenderId:guid}/Submissions/{submissionId:guid}/Review")]
public class TenderReviewController(
    ITenderReviewService tenderReviewService,
    ITenderReviewPageModelBuilder tenderReviewPageModelBuilder) : AuthenticatedControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Edit(Guid tenderId, Guid submissionId)
    {
        return View(await tenderReviewPageModelBuilder.BuildEditAsync(tenderId, submissionId, UserId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid tenderId,
        Guid submissionId,
        [Bind(Prefix = "Form")] TenderReviewFormViewModel form)
    {
        if (!ModelState.IsValid)
            return View(await tenderReviewPageModelBuilder.BuildEditAsync(
                tenderId,
                submissionId,
                UserId,
                form));

        try
        {
            await tenderReviewService.SaveReviewAsync(
                tenderId,
                submissionId,
                ToRatingInputs(form),
                UserId);

            TempData["TenderReviewSuccess"] = "De beoordeling is opgeslagen.";
            return RedirectToAction(nameof(Edit), new { tenderId, submissionId });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(await tenderReviewPageModelBuilder.BuildEditAsync(
                tenderId,
                submissionId,
                UserId,
                form,
                ex.Message));
        }
    }

    private static IEnumerable<TenderQuestionRatingInput> ToRatingInputs(TenderReviewFormViewModel form) =>
        form.Ratings
            .Where(rating => rating.Rating.HasValue)
            .Select(rating => new TenderQuestionRatingInput
            {
                QuestionId = rating.QuestionId,
                Rating = rating.Rating!.Value
            });
}
