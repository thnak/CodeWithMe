﻿using System.Diagnostics.CodeAnalysis;
using BusinessModels.Converter;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessModels.Utils;

public static class ClientSignalRHubExtensions
{
    public static HubConnection InitConnection(this Uri uri, bool useMessagePack = true)
    {
        var hubConnectionBuilder = new HubConnectionBuilder()
            .WithUrl(uri)
            .AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new ObjectIdConverter()); });

        if (useMessagePack)
        {
            hubConnectionBuilder.AddMessagePackProtocol(options => { options.SerializerOptions = GetMessagePackSerializerOptions(); });
        }

        return hubConnectionBuilder.Build();
    }

    public static HubConnection InitConnection(this HubConnectionBuilder builder, [StringSyntax(StringSyntaxAttribute.Uri)] string uri)
    {
        return builder.WithUrl(uri)
            .AddMessagePackProtocol(options => { options.SerializerOptions = GetMessagePackSerializerOptions(); })
            .WithAutomaticReconnect()
            .Build();
    }

    public static MessagePackSerializerOptions GetMessagePackSerializerOptions()
    {
        return MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(GetMessagePackFormatterResolvers()))
            .WithSecurity(MessagePackSecurity.UntrustedData);
    }

    public static IFormatterResolver[] GetMessagePackFormatterResolvers()
    {
        return
        [
            BuiltinResolver.Instance,
            AttributeFormatterResolver.Instance,
            // replace enum resolver
            DynamicEnumAsStringResolver.Instance,
            DynamicGenericResolver.Instance,
            DynamicUnionResolver.Instance,
            DynamicObjectResolver.Instance,

            PrimitiveObjectResolver.Instance,
            StandardResolver.Instance,
            NativeDecimalResolver.Instance,
            NativeGuidResolver.Instance,
            NativeDateTimeResolver.Instance,
            MongoObjectIdResolver.Instance
        ];
    }

    public static void RegisterResolvers()
    {
        StaticCompositeResolver.Instance.Register(GetMessagePackFormatterResolvers());
    }
}