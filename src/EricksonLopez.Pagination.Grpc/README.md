# EricksonLopez.Pagination.Grpc

This package provides gRPC extensions and types for cursor-based pagination.

## The `Any` Anti-Pattern in `CursorPagedList<T>`

In the `EricksonLopez.Pagination.Grpc` package, the `CursorPagedList<T>` relies on the Protobuf `google.protobuf.Any` type to represent polymorphic or generic item collections. 

### Why is `Any` used?
gRPC and Protocol Buffers do not natively support generics in the same way C# does. To send a list of items where the item type `T` is not known at the time the pagination message is defined, the `Any` type is used. `Any` allows mapping bytes to any message type, providing a generic wrapper.

### Drawbacks
While this pattern enables generic pagination responses across different gRPC services, it introduces several drawbacks:
- **Strong Typing Loss**: Clients receiving the message must unpack the `Any` type explicitly, knowing the expected type beforehand.
- **Client Compatibility**: Lightweight web clients (like gRPC-Web clients) or clients in languages without robust `Any` support might struggle to deserialize the payloads efficiently.
- **Payload Overhead**: The `Any` type includes a type URL string in every item, increasing the payload size compared to a strictly typed array.

### Mitigation & Alternatives
If your gRPC protocol or client landscape requires strictly typed messages without the overhead or complexity of `Any`, you should:
1. Define explicitly typed pagination responses in your `.proto` files (e.g., `UserPagedList`, `OrderPagedList`).
2. Map the domain `CursorPagedList<T>` into your strongly typed gRPC messages manually or via an object mapper.

Example of a strictly typed pagination response in Protobuf:
```proto
message UserPagedList {
  repeated User items = 1;
  int32 total_count = 2;
  bool has_next_page = 3;
  bool has_previous_page = 4;
  string start_cursor = 5;
  string end_cursor = 6;
}
```
