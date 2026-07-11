using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class MedicineTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var medicine = new Medicine(id, "Amoxicillin", "27658006", "Capsule");

        Assert.Equal(id, medicine.Id);
        Assert.Equal("Amoxicillin", medicine.Name);
        Assert.Equal("27658006", medicine.Sctid);
        Assert.Equal("Capsule", medicine.Form);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_ThrowsDomainException(string? name)
    {
        Assert.Throws<DomainException>(() => new Medicine(Guid.NewGuid(), name!, "27658006", "Capsule"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankSctid_ThrowsDomainException(string? sctid)
    {
        Assert.Throws<DomainException>(() => new Medicine(Guid.NewGuid(), "Amoxicillin", sctid!, "Capsule"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankForm_ThrowsDomainException(string? form)
    {
        Assert.Throws<DomainException>(() => new Medicine(Guid.NewGuid(), "Amoxicillin", "27658006", form!));
    }
}
