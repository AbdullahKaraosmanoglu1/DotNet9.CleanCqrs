using DotNet9.Application.Users.Abstractions;
using DotNet9.Application.Users.Commands.RegisterUser;
using FluentAssertions;
using NSubstitute;

namespace DotNet9.UnitTests.Users;

public class RegisterUserHandlerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly RegisterUserHandler _sut;

    public RegisterUserHandlerTests()
    {
        _sut = new RegisterUserHandler(_repo);
    }

    [Fact]
    public async Task Handle_Should_Create_User_And_Return_Id_When_Email_Not_Exists()
    {
        // arrange
        _repo.EmailExistsAsync("john@doe.com", Arg.Any<CancellationToken>())
             .Returns(false);

        var cmd = new RegisterUserCommand("john@doe.com", "john");

        // act
        var id = await _sut.Handle(cmd, CancellationToken.None);

        // assert
        id.Should().NotBeEmpty();
        await _repo.Received(1).AddAsync(Arg.Any<DotNet9.Domain.Users.User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Email_Already_Exists()
    {
        // arrange
        _repo.EmailExistsAsync("john@doe.com", Arg.Any<CancellationToken>())
             .Returns(true);

        var cmd = new RegisterUserCommand("john@doe.com", "john");

        // act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Email already in use");

        await _repo.DidNotReceive().AddAsync(Arg.Any<DotNet9.Domain.Users.User>(), Arg.Any<CancellationToken>());
    }
}
