using Domain.Entities;
using Domain.Enums;
using Presentation.Models.Tender;

namespace Presentation.Mappings;

public static class TenderMapper
{
    public static Tender ToEntity(TenderFormViewModel model) => new()
    {
        Title = model.Title,
        Description = model.Description,
        EndDate = model.EndDate,
        IsPublic = model.IsPublic,
        Status = TenderStatus.Design,
        OrganisationId = Guid.Empty
    };

    public static Tender ToEntity(TenderCreateRequest request) => new()
    {
        Title = request.Title,
        Description = request.Description,
        EndDate = request.EndDate,
        IsPublic = request.IsPublic,
        Status = TenderStatus.Design,
        OrganisationId = Guid.Empty
    };

    public static Tender ToEntity(TenderEditRequest request) => new()
    {
        Title = request.Title,
        Description = request.Description,
        EndDate = request.EndDate,
        IsPublic = request.IsPublic,
        Status = TenderStatus.Design,
        OrganisationId = Guid.Empty
    };

    public static TenderFormViewModel ToFormViewModel(Tender tender) => new()
    {
        Title = tender.Title,
        Description = tender.Description,
        EndDate = tender.EndDate,
        IsPublic = tender.IsPublic
    };

    public static TenderFormViewModel ToFormViewModel(TenderCreateRequest request) => new()
    {
        Title = request.Title,
        Description = request.Description,
        EndDate = request.EndDate,
        IsPublic = request.IsPublic
    };

    public static TenderFormViewModel ToFormViewModel(TenderEditRequest request) => new()
    {
        Title = request.Title,
        Description = request.Description,
        EndDate = request.EndDate,
        IsPublic = request.IsPublic
    };
}
