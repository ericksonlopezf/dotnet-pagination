// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Pagination.EntityFrameworkCore.Tests.Infrastructure.Builders;

/// <summary>
/// Fluent test data builder for <see cref="TestEntity"/>.
/// </summary>
public sealed class TestEntityBuilder
{
    private int _id = 1;
    private string _name = "Entity 1";
    private int? _nullableId;
    private string? _nullableString;
    private TestState _stateValue = TestState.One;
    private TestState _state = TestState.One;
    private Guid _guidValue = Guid.NewGuid();
    private int _additionalValue;
    private string? _name2;
    private string? _name3;
    private bool _booleanValue;
    private string? _name4;
    private string? _name5;
    private TestStruct _customStructValue;

    public TestEntityBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public TestEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public TestEntityBuilder WithNullableId(int? nullableId)
    {
        _nullableId = nullableId;
        return this;
    }

    public TestEntityBuilder WithNullableString(string? nullableString)
    {
        _nullableString = nullableString;
        return this;
    }

    public TestEntityBuilder WithState(TestState state)
    {
        _stateValue = state;
        _state = state;
        return this;
    }

    public TestEntityBuilder WithGuid(Guid guidValue)
    {
        _guidValue = guidValue;
        return this;
    }

    public TestEntityBuilder WithAdditionalValue(int value)
    {
        _additionalValue = value;
        return this;
    }

    public TestEntityBuilder WithName2(string? name2)
    {
        _name2 = name2;
        return this;
    }

    public TestEntityBuilder WithName3(string? name3)
    {
        _name3 = name3;
        return this;
    }

    public TestEntityBuilder WithName4(string? name4)
    {
        _name4 = name4;
        return this;
    }

    public TestEntityBuilder WithName5(string? name5)
    {
        _name5 = name5;
        return this;
    }

    public TestEntityBuilder WithBoolean(bool booleanValue)
    {
        _booleanValue = booleanValue;
        return this;
    }

    public TestEntityBuilder WithCustomStruct(int value)
    {
        _customStructValue = new TestStruct { Value = value };
        return this;
    }

    public TestEntity Build() => new()
    {
        Id = _id,
        Name = _name,
        NullableId = _nullableId,
        NullableString = _nullableString,
        StateValue = _stateValue,
        State = _state,
        GuidValue = _guidValue,
        AdditionalValue = _additionalValue,
        Name2 = _name2,
        Name3 = _name3,
        BooleanValue = _booleanValue,
        Name4 = _name4,
        Name5 = _name5,
        CustomStructValue = _customStructValue
    };

    public static implicit operator TestEntity(TestEntityBuilder builder) => builder.Build();
}

/// <summary>
/// Fluent test data builder for <see cref="TypeEntity"/>.
/// </summary>
public sealed class TypeEntityBuilder
{
    private long _id = 1;
    private Guid _guidVal = Guid.NewGuid();
    private DateTimeOffset _dateTimeOffsetVal = DateTimeOffset.UtcNow;
    private DateTime _dateTimeVal = DateTime.UtcNow;
    private string _stringVal = "Default";

    public TypeEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public TypeEntityBuilder WithGuid(Guid guidValue)
    {
        _guidVal = guidValue;
        return this;
    }

    public TypeEntityBuilder WithDateTimeOffset(DateTimeOffset dto)
    {
        _dateTimeOffsetVal = dto;
        return this;
    }

    public TypeEntityBuilder WithDateTime(DateTime dt)
    {
        _dateTimeVal = dt;
        return this;
    }

    public TypeEntityBuilder WithString(string val)
    {
        _stringVal = val;
        return this;
    }

    public TypeEntity Build() => new()
    {
        Id = _id,
        GuidVal = _guidVal,
        DateTimeOffsetVal = _dateTimeOffsetVal,
        DateTimeVal = _dateTimeVal,
        StringVal = _stringVal
    };

    public static implicit operator TypeEntity(TypeEntityBuilder builder) => builder.Build();
}
