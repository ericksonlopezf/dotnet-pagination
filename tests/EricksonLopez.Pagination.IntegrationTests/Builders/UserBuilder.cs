// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.IntegrationTests;

/// <summary>
/// Fluent test data builder for integration test <see cref="User"/> entity.
/// </summary>
public sealed class UserBuilder
{
    private int _id;
    private string _name = "User";
    private int _age = 25;
    private bool _isActive = true;

    public UserBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserBuilder WithAge(int age)
    {
        _age = age;
        return this;
    }

    public UserBuilder WithActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public User Build() => new()
    {
        Id = _id,
        Name = _name,
        Age = _age,
        IsActive = _isActive
    };

    public static implicit operator User(UserBuilder builder) => builder.Build();
}
