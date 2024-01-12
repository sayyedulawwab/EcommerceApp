﻿using Ecommerce.Domain.Abstractions;
using MediatR;

namespace Ecommerce.Application.Abstactions.Messaging;
public interface ICommand : IRequest<Result>, IBaseCommand
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

public interface IBaseCommand
{
}