using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Provides common <see cref="TypeIdentity"/> instances for use by source generators.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
[SuppressMessage("Naming", "CA1720:Identifier contains type name")]
public static partial class PurviewTypeLibrary
{
	/// <summary>
	/// Common types from the <c>System</c> namespace hierarchy.
	/// </summary>
	public static class System
	{
		/// <summary>
		/// The <c>System</c> namespace.
		/// </summary>
		public const string Namespace = "System";

		/// <summary>
		/// The <c>System.Attribute</c> type.
		/// </summary>
		public static readonly TypeIdentity Attribute = TypeIdentity.Create<Attribute>();

		/// <summary>
		/// The <c>System.AttributeUsageAttribute</c> type.
		/// </summary>
		public static readonly TypeIdentity AttributeUsageAttribute = TypeIdentity.Create<AttributeUsageAttribute>();

		/// <summary>
		/// The <c>System.FlagsAttribute</c> type.
		/// </summary>
		public static readonly TypeIdentity FlagsAttribute = TypeIdentity.Create<FlagsAttribute>();

		/// <summary>
		/// The <c>System.ObsoleteAttribute</c> type.
		/// </summary>
		public static readonly TypeIdentity ObsoleteAttribute = TypeIdentity.Create<ObsoleteAttribute>();

		/// <summary>
		/// The <c>System.SerializableAttribute</c> type.
		/// </summary>
		public static readonly TypeIdentity SerializableAttribute = TypeIdentity.Create<SerializableAttribute>();

		/// <summary>
		/// The <c>System.IComparable</c> type. The generic
		/// <c>System.IComparable{T}</c> form has arity 1.
		/// </summary>
		public static readonly TypeIdentity IComparable = TypeIdentity.Create<IComparable>();

		/// <summary>
		/// The generic <c>System.IEquatable{T}</c> type.
		/// The type has arity 1.
		/// </summary>
		public static readonly TypeIdentity IEquatable = new(nameof(IEquatable), Namespace, 1);

		/// <summary>
		/// The generic <c>System.Nullable{T}</c> type.
		/// The type has arity 1.
		/// </summary>
		public static readonly TypeIdentity Nullable = new(nameof(Nullable), Namespace, 1);

		/// <summary>
		/// The <c>System.Array</c> type.
		/// </summary>
		public static readonly TypeIdentity Array = TypeIdentity.Create<Array>();

		/// <summary>
		/// The <c>System.Type</c> type.
		/// </summary>
		public static readonly TypeIdentity Type = TypeIdentity.Create<Type>();

		/// <summary>
		/// The <c>System.Enum</c> type.
		/// </summary>
		public static readonly TypeIdentity Enum = TypeIdentity.Create<Enum>();

		/// <summary>
		/// The <c>System.ValueType</c> type.
		/// </summary>
		public static readonly TypeIdentity ValueType = TypeIdentity.Create<ValueType>();

		/// <summary>
		/// The <c>System.Delegate</c> type.
		/// </summary>
		public static readonly TypeIdentity Delegate = TypeIdentity.Create<Delegate>();

		/// <summary>
		/// The <c>System.MulticastDelegate</c> type.
		/// </summary>
		public static readonly TypeIdentity MulticastDelegate = TypeIdentity.Create<MulticastDelegate>();

		/// <summary>
		/// The <c>System.Uri</c> type.
		/// </summary>
		public static readonly TypeIdentity Uri = TypeIdentity.Create<Uri>();

		/// <summary>
		/// The <c>System.Version</c> type.
		/// </summary>
		public static readonly TypeIdentity Version = TypeIdentity.Create<Version>();

		/// <summary>
		/// The <c>System.IFormattable</c> type.
		/// </summary>
		public static readonly TypeIdentity IFormattable = TypeIdentity.Create<IFormattable>();

		/// <summary>
		/// The <c>System.IConvertible</c> type.
		/// </summary>
		public static readonly TypeIdentity IConvertible = TypeIdentity.Create<IConvertible>();

		/// <summary>
		/// The generic <c>System.Lazy{T}</c> type.
		/// The type has arity 1.
		/// </summary>
		public static readonly TypeIdentity Lazy = new(nameof(Lazy), Namespace, 1);

		/// <summary>
		/// The <c>System.Guid</c> type.
		/// </summary>
		public static readonly TypeIdentity Guid = TypeIdentity.Create<Guid>();

		/// <summary>
		/// The <see langword="bool"/> type.
		/// </summary>
		public static readonly TypeIdentity Boolean = TypeIdentity.Create<bool>();

		/// <summary>
		/// The <see langword="byte"/> type.
		/// </summary>
		public static readonly TypeIdentity Byte = TypeIdentity.Create<byte>();

		/// <summary>
		/// The <see langword="sbyte"/> type.
		/// </summary>
		public static readonly TypeIdentity SByte = TypeIdentity.Create<sbyte>();

		/// <summary>
		/// The <see langword="char"/> type.
		/// </summary>
		public static readonly TypeIdentity Char = TypeIdentity.Create<char>();

		/// <summary>
		/// The <see langword="decimal"/> type.
		/// </summary>
		public static readonly TypeIdentity Decimal = TypeIdentity.Create<decimal>();

		/// <summary>
		/// The <see langword="double"/> type.
		/// </summary>
		public static readonly TypeIdentity Double = TypeIdentity.Create<double>();

		/// <summary>
		/// The <see langword="float"/> type.
		/// </summary>
		public static readonly TypeIdentity Float = TypeIdentity.Create<float>();

		/// <summary>
		/// The <see langword="int"/> type.
		/// </summary>
		public static readonly TypeIdentity Int32 = TypeIdentity.Create<int>();

		/// <summary>
		/// The <see langword="uint"/> type.
		/// </summary>
		public static readonly TypeIdentity UInt32 = TypeIdentity.Create<uint>();

		/// <summary>
		/// The <see langword="long"/> type.
		/// </summary>
		public static readonly TypeIdentity Int64 = TypeIdentity.Create<long>();

		/// <summary>
		/// The <see langword="ulong"/> type.
		/// </summary>
		public static readonly TypeIdentity UInt64 = TypeIdentity.Create<ulong>();

		/// <summary>
		/// The <see langword="short"/> type.
		/// </summary>
		public static readonly TypeIdentity Int16 = TypeIdentity.Create<short>();

		/// <summary>
		/// The <see langword="ushort"/> type.
		/// </summary>
		public static readonly TypeIdentity UInt16 = TypeIdentity.Create<ushort>();

		/// <summary>
		/// The <see langword="string"/> type.
		/// </summary>
		public static readonly TypeIdentity String = TypeIdentity.Create<string>();

		/// <summary>
		/// The <see langword="object"/> type.
		/// </summary>
		public static readonly TypeIdentity Object = TypeIdentity.Create<object>();

		/// <summary>
		/// The <see langword="void"/> type.
		/// </summary>
		public static readonly TypeIdentity Void = new("void", null);

		/// <summary>
		/// The C# <c>null</c> literal, presented as an identity for use in value positions.
		/// </summary>
		/// <remarks>
		/// Renders as <c>null</c> and converts to the <c>"null"</c> expression where a
		/// string value is expected, such as an initializer, default value, argument or
		/// return value. It is not a type and is rejected in type positions.
		/// </remarks>
		public static readonly TypeIdentity Null = TypeIdentity.Null;

		/// <summary>
		/// The <see langword="nint"/> type.
		/// </summary>
		public static readonly TypeIdentity IntPtr = TypeIdentity.Create<nint>();

		/// <summary>
		/// The <see langword="nuint"/> type.
		/// </summary>
		public static readonly TypeIdentity UIntPtr = TypeIdentity.Create<nuint>();

		/// <summary>
		/// The <c>System.Action</c> delegate family. Generic forms have arities from 1 through 16.
		/// </summary>
		/// <remarks>
		/// If you require a normal action with no arity, call this with the <see cref="TypeIdentity.WithArity(int)"/> method
		/// and a value of <c>0</c>.
		/// </remarks>
		public static readonly TypeIdentity Action = new(nameof(Action), Namespace, 1);

		/// <summary>
		/// The generic <c>System.Func{TResult}</c> delegate family.
		/// Generic forms have arities from 1 through 17.
		/// </summary>
		public static readonly TypeIdentity Func = new(nameof(Func), Namespace, 1);

		/// <summary>
		/// The generic <c>System.Predicate{T}</c> delegate type.
		/// The type has arity 1.
		/// </summary>
		public static readonly TypeIdentity Predicate = new(nameof(Predicate), Namespace, 1);

		/// <summary>
		/// The generic <c>System.Comparison{T}</c> delegate type.
		/// The type has arity 1.
		/// </summary>
		public static readonly TypeIdentity Comparison = new(nameof(Comparison), Namespace, 1);

		/// <summary>
		/// The <c>System.Exception</c> type.
		/// </summary>
		public static readonly TypeIdentity Exception = TypeIdentity.Create<Exception>();

		/// <summary>
		/// The <c>System.ArgumentException</c> type.
		/// </summary>
		public static readonly TypeIdentity ArgumentException = TypeIdentity.Create<ArgumentException>();

		/// <summary>
		/// The <c>System.ArgumentNullException</c> type.
		/// </summary>
		public static readonly TypeIdentity ArgumentNullException = TypeIdentity.Create<ArgumentNullException>();

		/// <summary>
		/// The <c>System.ArgumentOutOfRangeException</c> type.
		/// </summary>
		public static readonly TypeIdentity ArgumentOutOfRangeException =
			TypeIdentity.Create<ArgumentOutOfRangeException>();

		/// <summary>
		/// The <c>System.InvalidOperationException</c> type.
		/// </summary>
		public static readonly TypeIdentity InvalidOperationException =
			TypeIdentity.Create<InvalidOperationException>();

		/// <summary>
		/// The <c>System.NotSupportedException</c> type.
		/// </summary>
		public static readonly TypeIdentity NotSupportedException = TypeIdentity.Create<NotSupportedException>();

		/// <summary>
		/// The <c>System.NotImplementedException</c> type.
		/// </summary>
		public static readonly TypeIdentity NotImplementedException = TypeIdentity.Create<NotImplementedException>();

		/// <summary>
		/// The <c>System.FormatException</c> type.
		/// </summary>
		public static readonly TypeIdentity FormatException = TypeIdentity.Create<FormatException>();

		/// <summary>
		/// The <c>System.IDisposable</c> type.
		/// </summary>
		public static readonly TypeIdentity IDisposable = TypeIdentity.Create<IDisposable>();

		/// <summary>
		/// The <c>System.IAsyncDisposable</c> type.
		/// </summary>
		public static readonly TypeIdentity IAsyncDisposable = new(nameof(IAsyncDisposable), Namespace);

		/// <summary>
		/// The <c>System.DateTimeOffset</c> type.
		/// </summary>
		public static readonly TypeIdentity DateTimeOffset = TypeIdentity.Create<DateTimeOffset>();

		/// <summary>
		/// The <c>System.DateTime</c> type.
		/// </summary>
		public static readonly TypeIdentity DateTime = TypeIdentity.Create<DateTime>();

		/// <summary>
		/// The <c>System.TimeSpan</c> type.
		/// </summary>
		public static readonly TypeIdentity TimeSpan = TypeIdentity.Create<TimeSpan>();

		/// <summary>
		/// The <c>System.DateOnly</c> type.
		/// </summary>
		public static readonly TypeIdentity DateOnly = new(nameof(DateOnly), Namespace);

		/// <summary>
		/// The <c>System.TimeOnly</c> type.
		/// </summary>
		public static readonly TypeIdentity TimeOnly = new(nameof(TimeOnly), Namespace);

		/// <summary>
		/// Common types from the <c>System.Threading</c> namespace hierarchy.
		/// </summary>
		public static class Threading
		{
			/// <summary>
			/// The <c>System.Threading</c> namespace.
			/// </summary>
			public const string Namespace = "System.Threading";

			/// <summary>
			/// The <c>System.Threading.CancellationToken</c> type.
			/// </summary>
			public static readonly TypeIdentity CancellationToken = TypeIdentity.Create<CancellationToken>();

			/// <summary>
			/// The <c>System.Threading.CancellationTokenSource</c> type.
			/// </summary>
			public static readonly TypeIdentity CancellationTokenSource =
				TypeIdentity.Create<CancellationTokenSource>();

			/// <summary>
			/// The <c>System.Threading.SemaphoreSlim</c> type.
			/// </summary>
			public static readonly TypeIdentity SemaphoreSlim = TypeIdentity.Create<SemaphoreSlim>();

			/// <summary>
			/// The <c>System.Threading.SynchronizationContext</c> type.
			/// </summary>
			public static readonly TypeIdentity SynchronizationContext = TypeIdentity.Create<SynchronizationContext>();

			/// <summary>
			/// The <c>System.Threading.Interlocked</c> type.
			/// </summary>
			public static readonly TypeIdentity Interlocked = new(nameof(Interlocked), Namespace);

			/// <summary>
			/// The <c>System.Threading.Monitor</c> type.
			/// </summary>
			public static readonly TypeIdentity Monitor = new(nameof(Monitor), Namespace);

			/// <summary>
			/// The generic <c>System.Threading.AsyncLocal{T}</c> type.
			/// The type has arity 1.
			/// </summary>
			public static readonly TypeIdentity AsyncLocal = new(nameof(AsyncLocal), Namespace, 1);

			public static class Tasks
			{
				/// <summary>
				/// The <c>System.Threading.Tasks</c> namespace.
				/// </summary>
				public const string Namespace = "System.Threading.Tasks";

				/// <summary>
				/// The <c>System.Threading.Tasks.Task</c> type.
				/// The generic <c>Task{TResult}</c> form has arity 1.
				/// </summary>
				public static readonly TypeIdentity Task = TypeIdentity.Create<Task>();

				/// <summary>
				/// The <c>System.Threading.Tasks.ValueTask</c> type.
				/// The generic <c>ValueTask{TResult}</c> form has arity 1.
				/// </summary>
				public static readonly TypeIdentity ValueTask = TypeIdentity.Create<ValueTask>();

				/// <summary>
				/// The <c>System.Threading.Tasks.TaskFactory</c> type.
				/// The generic <c>TaskFactory{TResult}</c> form has arity 1.
				/// </summary>
				public static readonly TypeIdentity TaskFactory = new(nameof(TaskFactory), Namespace, 1);

				/// <summary>
				/// The <c>System.Threading.Tasks.TaskScheduler</c> type.
				/// </summary>
				public static readonly TypeIdentity TaskScheduler = new(nameof(TaskScheduler), Namespace);

				/// <summary>
				/// The <c>System.Threading.Tasks.TaskStatus</c> type.
				/// </summary>
				public static readonly TypeIdentity TaskStatus = new(nameof(TaskStatus), Namespace);

				/// <summary>
				/// The <c>System.Threading.Tasks.TaskCreationOptions</c> type.
				/// </summary>
				public static readonly TypeIdentity TaskCreationOptions = new(nameof(TaskCreationOptions), Namespace);

				/// <summary>
				/// The <c>System.Threading.Tasks.TaskContinuationOptions</c> type.
				/// </summary>
				public static readonly TypeIdentity TaskContinuationOptions = new(
					nameof(TaskContinuationOptions),
					Namespace
				);

				/// <summary>
				/// The generic <c>System.Threading.Tasks.TaskCompletionSource{TResult}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity TaskCompletionSource = new(
					nameof(TaskCompletionSource),
					Namespace,
					1
				);
			}
		}

		/// <summary>
		/// Common types from the <c>System.Globalization</c> namespace.
		/// </summary>
		public static partial class Globalization
		{
			/// <summary>
			/// The <c>System.Globalization</c> namespace.
			/// </summary>
			public const string Namespace = "System.Globalization";

			/// <summary>
			/// The <c>System.Globalization.CultureInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity CultureInfo =
				TypeIdentity.Create<global::System.Globalization.CultureInfo>();

			/// <summary>
			/// The <c>System.Globalization.CultureNotFoundException</c> type.
			/// </summary>
			public static readonly TypeIdentity CultureNotFoundException =
				TypeIdentity.Create<global::System.Globalization.CultureNotFoundException>();

			/// <summary>
			/// The <c>System.Globalization.NumberStyles</c> type.
			/// </summary>
			public static readonly TypeIdentity NumberStyles =
				TypeIdentity.Create<global::System.Globalization.NumberStyles>();

			/// <summary>
			/// The <c>System.Globalization.DateTimeStyles</c> type.
			/// </summary>
			public static readonly TypeIdentity DateTimeStyles =
				TypeIdentity.Create<global::System.Globalization.DateTimeStyles>();

			/// <summary>
			/// The <c>System.Globalization.TextInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity TextInfo = TypeIdentity.Create<global::System.Globalization.TextInfo>();

			/// <summary>
			/// The <c>System.Globalization.DateTimeFormatInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity DateTimeFormatInfo =
				TypeIdentity.Create<global::System.Globalization.DateTimeFormatInfo>();

			/// <summary>
			/// The <c>System.Globalization.NumberFormatInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity NumberFormatInfo =
				TypeIdentity.Create<global::System.Globalization.NumberFormatInfo>();
		}

		/// <summary>
		/// Common types from the <c>System.Collections</c> namespace hierarchy.
		/// </summary>
		public static partial class Collections
		{
			/// <summary>
			/// The <c>System.Collections</c> namespace.
			/// </summary>
			public const string Namespace = "System.Collections";

			/// <summary>
			/// The <c>System.Collections.IEnumerable</c> type.
			/// </summary>
			public static readonly TypeIdentity IEnumerable =
				TypeIdentity.Create<global::System.Collections.IEnumerable>();

			/// <summary>
			/// The <c>System.Collections.IEnumerator</c> type.
			/// </summary>
			public static readonly TypeIdentity IEnumerator =
				TypeIdentity.Create<global::System.Collections.IEnumerator>();

			/// <summary>
			/// The <c>System.Collections.IDictionary</c> type.
			/// </summary>
			public static readonly TypeIdentity IDictionary =
				TypeIdentity.Create<global::System.Collections.IDictionary>();

			/// <summary>
			/// The <c>System.Collections.ICollection</c> type.
			/// </summary>
			public static readonly TypeIdentity ICollection =
				TypeIdentity.Create<global::System.Collections.ICollection>();

			/// <summary>
			/// The <c>System.Collections.IList</c> type.
			/// </summary>
			public static readonly TypeIdentity IList = TypeIdentity.Create<global::System.Collections.IList>();

			/// <summary>
			/// The <c>System.Collections.ArrayList</c> type.
			/// </summary>
			public static readonly TypeIdentity ArrayList = TypeIdentity.Create<global::System.Collections.ArrayList>();

			/// <summary>
			/// The <c>System.Collections.SortedList</c> type.
			/// </summary>
			public static readonly TypeIdentity SortedList =
				TypeIdentity.Create<global::System.Collections.SortedList>();

			/// <summary>
			/// The <c>System.Collections.Queue</c> type.
			/// </summary>
			public static readonly TypeIdentity Queue = TypeIdentity.Create<global::System.Collections.Queue>();

			/// <summary>
			/// The <c>System.Collections.Stack</c> type.
			/// </summary>
			public static readonly TypeIdentity Stack = TypeIdentity.Create<global::System.Collections.Stack>();

			/// <summary>
			/// The <c>System.Collections.ReadOnlyCollectionBase</c> type.
			/// </summary>
			public static readonly TypeIdentity ReadOnlyCollectionBase =
				TypeIdentity.Create<global::System.Collections.ReadOnlyCollectionBase>();

			/// <summary>
			/// Common types from the <c>System.Collections.Generic</c> namespace.
			/// </summary>
			public static partial class Generic
			{
				/// <summary>
				/// The <c>System.Collections.Generic</c> namespace.
				/// </summary>
				public const string Namespace = "System.Collections.Generic";

				/// <summary>
				/// The generic <c>System.Collections.Generic.IEnumerable{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IEnumerable = new(nameof(IEnumerable), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IEnumerator{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IEnumerator = new(nameof(IEnumerator), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.ICollection{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ICollection = new(nameof(ICollection), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IList{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IList = new(nameof(IList), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IReadOnlyCollection{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IReadOnlyCollection = new(
					nameof(IReadOnlyCollection),
					Namespace,
					1
				);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IReadOnlyList{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IReadOnlyList = new(nameof(IReadOnlyList), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.ISet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ISet = new(nameof(ISet), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IReadOnlySet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IReadOnlySet = new(nameof(IReadOnlySet), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity IDictionary = new(nameof(IDictionary), Namespace, 2);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity IReadOnlyDictionary = new(
					nameof(IReadOnlyDictionary),
					Namespace,
					2
				);

				/// <summary>
				/// The generic <c>System.Collections.Generic.List{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity List = new(nameof(List), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.Dictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity Dictionary = new(nameof(Dictionary), Namespace, 2);

				/// <summary>
				/// The generic <c>System.Collections.Generic.HashSet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity HashSet = new(nameof(HashSet), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.SortedDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity SortedDictionary = new(nameof(SortedDictionary), Namespace, 2);

				/// <summary>
				/// The generic <c>System.Collections.Generic.SortedSet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity SortedSet = new(nameof(SortedSet), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.SortedList{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity SortedList = new(nameof(SortedList), Namespace, 2);

				/// <summary>
				/// The generic <c>System.Collections.Generic.LinkedList{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity LinkedList = new(nameof(LinkedList), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.LinkedListNode{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity LinkedListNode = new(nameof(LinkedListNode), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.Queue{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity Queue = new(nameof(Queue), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.Stack{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity Stack = new(nameof(Stack), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.KeyValuePair{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity KeyValuePair = new(nameof(KeyValuePair), Namespace, 2);

				/// <summary>
				/// The <c>System.Collections.Generic.KeyNotFoundException</c> type.
				/// </summary>
				public static readonly TypeIdentity KeyNotFoundException = new(nameof(KeyNotFoundException), Namespace);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IComparer{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IComparer = new(nameof(IComparer), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.IEqualityComparer{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IEqualityComparer = new(nameof(IEqualityComparer), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.Comparer{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity Comparer = new(nameof(Comparer), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Generic.EqualityComparer{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity EqualityComparer = new(nameof(EqualityComparer), Namespace, 1);
			}

			/// <summary>
			/// Common types from the <c>System.Collections.Concurrent</c> namespace.
			/// </summary>
			public static partial class Concurrent
			{
				/// <summary>
				/// The <c>System.Collections.Concurrent</c> namespace.
				/// </summary>
				public const string Namespace = "System.Collections.Concurrent";

				/// <summary>
				/// The generic
				/// <c>System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity ConcurrentDictionary = new(
					nameof(ConcurrentDictionary),
					Namespace,
					2
				);

				/// <summary>
				/// The generic <c>System.Collections.Concurrent.ConcurrentQueue{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ConcurrentQueue = new(nameof(ConcurrentQueue), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Concurrent.ConcurrentStack{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ConcurrentStack = new(nameof(ConcurrentStack), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Concurrent.ConcurrentBag{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ConcurrentBag = new(nameof(ConcurrentBag), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Concurrent.BlockingCollection{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity BlockingCollection = new(nameof(BlockingCollection), Namespace, 1);

				/// <summary>
				/// The generic
				/// <c>System.Collections.Concurrent.IProducerConsumerCollection{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IProducerConsumerCollection = new(
					nameof(IProducerConsumerCollection),
					Namespace,
					1
				);
			}

			/// <summary>
			/// Common types from the <c>System.Collections.ObjectModel</c> namespace.
			/// </summary>
			public static partial class ObjectModel
			{
				/// <summary>
				/// The <c>System.Collections.ObjectModel</c> namespace.
				/// </summary>
				public const string Namespace = "System.Collections.ObjectModel";

				/// <summary>
				/// The generic <c>System.Collections.ObjectModel.Collection{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity Collection = new(nameof(Collection), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.ObjectModel.ReadOnlyCollection{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ReadOnlyCollection = new(nameof(ReadOnlyCollection), Namespace, 1);

				/// <summary>
				/// The generic
				/// <c>System.Collections.ObjectModel.ReadOnlyDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity ReadOnlyDictionary = new(nameof(ReadOnlyDictionary), Namespace, 2);

				/// <summary>
				/// The generic <c>System.Collections.ObjectModel.KeyedCollection{TKey, TItem}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity KeyedCollection = new(nameof(KeyedCollection), Namespace, 2);

				/// <summary>
				/// The generic
				/// <c>System.Collections.ObjectModel.ObservableCollection{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ObservableCollection = new(
					nameof(ObservableCollection),
					Namespace,
					1
				);
			}

			/// <summary>
			/// Common types from the <c>System.Collections.Immutable</c> namespace.
			/// </summary>
			public static partial class Immutable
			{
				/// <summary>
				/// The <c>System.Collections.Immutable</c> namespace.
				/// </summary>
				public const string Namespace = "System.Collections.Immutable";

				/// <summary>
				/// The generic <c>System.Collections.Immutable.ImmutableArray{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ImmutableArray = new(nameof(ImmutableArray), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.ImmutableArray{T}.Builder</c> type.
				/// </summary>
				public static readonly TypeIdentity ImmutableArrayBuilder = new(typeof(ImmutableArray<>.Builder));

				/// <summary>
				/// The generic <c>System.Collections.Immutable.ImmutableList{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ImmutableList = new(nameof(ImmutableList), Namespace, 1);

				/// <summary>
				/// The generic
				/// <c>System.Collections.Immutable.ImmutableDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity ImmutableDictionary = new(
					nameof(ImmutableDictionary),
					Namespace,
					2
				);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.ImmutableHashSet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ImmutableHashSet = new(nameof(ImmutableHashSet), Namespace, 1);

				/// <summary>
				/// The generic
				/// <c>System.Collections.Immutable.ImmutableSortedDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity ImmutableSortedDictionary = new(
					nameof(ImmutableSortedDictionary),
					Namespace,
					2
				);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.ImmutableSortedSet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ImmutableSortedSet = new(nameof(ImmutableSortedSet), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.ImmutableStack{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ImmutableStack = new(nameof(ImmutableStack), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.ImmutableQueue{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity ImmutableQueue = new(nameof(ImmutableQueue), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.IImmutableList{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IImmutableList = new(nameof(IImmutableList), Namespace, 1);

				/// <summary>
				/// The generic
				/// <c>System.Collections.Immutable.IImmutableDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity IImmutableDictionary = new(
					nameof(IImmutableDictionary),
					Namespace,
					2
				);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.IImmutableSet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IImmutableSet = new(nameof(IImmutableSet), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.IImmutableStack{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IImmutableStack = new(nameof(IImmutableStack), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Immutable.IImmutableQueue{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IImmutableQueue = new(nameof(IImmutableQueue), Namespace, 1);
			}

			/// <summary>
			/// Common types from the <c>System.Collections.Frozen</c> namespace.
			/// </summary>
			public static partial class Frozen
			{
				/// <summary>
				/// The <c>System.Collections.Frozen</c> namespace.
				/// </summary>
				public const string Namespace = "System.Collections.Frozen";

				/// <summary>
				/// The generic <c>System.Collections.Frozen.FrozenSet{T}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity FrozenSet = new(nameof(FrozenSet), Namespace, 1);

				/// <summary>
				/// The generic <c>System.Collections.Frozen.FrozenDictionary{TKey, TValue}</c> type.
				/// The type has arity 2.
				/// </summary>
				public static readonly TypeIdentity FrozenDictionary = new(nameof(FrozenDictionary), Namespace, 2);
			}
		}

		/// <summary>
		/// Common types from the <c>System.Linq</c> namespace.
		/// </summary>
		public static partial class Linq
		{
			/// <summary>
			/// The <c>System.Linq</c> namespace.
			/// </summary>
			public const string Namespace = "System.Linq";

			/// <summary>
			/// The <c>System.Linq.Enumerable</c> type.
			/// </summary>
			public static readonly TypeIdentity Enumerable = new(nameof(Enumerable), Namespace);

			/// <summary>
			/// The <c>System.Linq.Queryable</c> type.
			/// </summary>
			public static readonly TypeIdentity Queryable = new(nameof(Queryable), Namespace);

			/// <summary>
			/// The generic <c>System.Linq.IQueryable{T}</c> type.
			/// The type has arity 1.
			/// </summary>
			public static readonly TypeIdentity IQueryable = new(nameof(IQueryable), Namespace, 1);

			/// <summary>
			/// The generic <c>System.Linq.IOrderedQueryable{T}</c> type.
			/// The type has arity 1.
			/// </summary>
			public static readonly TypeIdentity IOrderedQueryable = new(nameof(IOrderedQueryable), Namespace, 1);

			/// <summary>
			/// The generic <c>System.Linq.IGrouping{TKey, TElement}</c> type.
			/// The type has arity 2.
			/// </summary>
			public static readonly TypeIdentity IGrouping = new(nameof(IGrouping), Namespace, 2);

			/// <summary>
			/// The generic <c>System.Linq.ILookup{TKey, TElement}</c> type.
			/// The type has arity 2.
			/// </summary>
			public static readonly TypeIdentity ILookup = new(nameof(ILookup), Namespace, 2);

			/// <summary>
			/// The generic <c>System.Linq.IOrderedEnumerable{TElement}</c> type.
			/// The type has arity 1.
			/// </summary>
			public static readonly TypeIdentity IOrderedEnumerable = new(nameof(IOrderedEnumerable), Namespace, 1);
		}

		/// <summary>
		/// Common types from the <c>System.IO</c> namespace.
		/// </summary>
		public static partial class IO
		{
			/// <summary>
			/// The <c>System.IO</c> namespace.
			/// </summary>
			public const string Namespace = "System.IO";

			/// <summary>
			/// The <c>System.IO.Stream</c> type.
			/// </summary>
			public static readonly TypeIdentity Stream = TypeIdentity.Create<Stream>();

			/// <summary>
			/// The <c>System.IO.MemoryStream</c> type.
			/// </summary>
			public static readonly TypeIdentity MemoryStream = TypeIdentity.Create<MemoryStream>();

			/// <summary>
			/// The <c>System.IO.TextReader</c> type.
			/// </summary>
			public static readonly TypeIdentity TextReader = TypeIdentity.Create<TextReader>();

			/// <summary>
			/// The <c>System.IO.TextWriter</c> type.
			/// </summary>
			public static readonly TypeIdentity TextWriter = TypeIdentity.Create<TextWriter>();

			/// <summary>
			/// The <c>System.IO.StringReader</c> type.
			/// </summary>
			public static readonly TypeIdentity StringReader = TypeIdentity.Create<StringReader>();

			/// <summary>
			/// The <c>System.IO.StringWriter</c> type.
			/// </summary>
			public static readonly TypeIdentity StringWriter = TypeIdentity.Create<StringWriter>();

			/// <summary>
			/// The <c>System.IO.StreamReader</c> type.
			/// </summary>
			public static readonly TypeIdentity StreamReader = TypeIdentity.Create<StreamReader>();

			/// <summary>
			/// The <c>System.IO.StreamWriter</c> type.
			/// </summary>
			public static readonly TypeIdentity StreamWriter = TypeIdentity.Create<StreamWriter>();

			/// <summary>
			/// The <c>System.IO.BinaryReader</c> type.
			/// </summary>
			public static readonly TypeIdentity BinaryReader = TypeIdentity.Create<BinaryReader>();

			/// <summary>
			/// The <c>System.IO.BinaryWriter</c> type.
			/// </summary>
			public static readonly TypeIdentity BinaryWriter = TypeIdentity.Create<BinaryWriter>();

			/// <summary>
			/// The <c>System.IO.IOException</c> type.
			/// </summary>
			public static readonly TypeIdentity IOException = TypeIdentity.Create<IOException>();

			/// <summary>
			/// The <c>System.IO.FileInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity FileInfo = TypeIdentity.Create<FileInfo>();

			/// <summary>
			/// The <c>System.IO.DirectoryInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity DirectoryInfo = TypeIdentity.Create<DirectoryInfo>();

			/// <summary>
			/// The <c>System.IO.File</c> type.
			/// </summary>
			public static readonly TypeIdentity File = new(nameof(File), Namespace);

			/// <summary>
			/// The <c>System.IO.Directory</c> type.
			/// </summary>
			public static readonly TypeIdentity Directory = new(nameof(Directory), Namespace);

			/// <summary>
			/// The <c>System.IO.Path</c> type.
			/// </summary>
			public static readonly TypeIdentity Path = new(nameof(Path), Namespace);
		}

		/// <summary>
		/// Common types from the <c>System.Reflection</c> namespace.
		/// </summary>
		public static partial class Reflection
		{
			/// <summary>
			/// The <c>System.Reflection</c> namespace.
			/// </summary>
			public const string Namespace = "System.Reflection";

			/// <summary>
			/// The <c>System.Reflection.Assembly</c> type.
			/// </summary>
			public static readonly TypeIdentity Assembly = TypeIdentity.Create<global::System.Reflection.Assembly>();

			/// <summary>
			/// The <c>System.Reflection.MemberInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity MemberInfo =
				TypeIdentity.Create<global::System.Reflection.MemberInfo>();

			/// <summary>
			/// The <c>System.Reflection.MethodInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity MethodInfo =
				TypeIdentity.Create<global::System.Reflection.MethodInfo>();

			/// <summary>
			/// The <c>System.Reflection.PropertyInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity PropertyInfo =
				TypeIdentity.Create<global::System.Reflection.PropertyInfo>();

			/// <summary>
			/// The <c>System.Reflection.FieldInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity FieldInfo = TypeIdentity.Create<global::System.Reflection.FieldInfo>();

			/// <summary>
			/// The <c>System.Reflection.ConstructorInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity ConstructorInfo =
				TypeIdentity.Create<global::System.Reflection.ConstructorInfo>();

			/// <summary>
			/// The <c>System.Reflection.ParameterInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity ParameterInfo =
				TypeIdentity.Create<global::System.Reflection.ParameterInfo>();

			/// <summary>
			/// The <c>System.Reflection.BindingFlags</c> type.
			/// </summary>
			public static readonly TypeIdentity BindingFlags =
				TypeIdentity.Create<global::System.Reflection.BindingFlags>();

			/// <summary>
			/// The <c>System.Reflection.TypeInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity TypeInfo = TypeIdentity.Create<global::System.Reflection.TypeInfo>();

			/// <summary>
			/// The <c>System.Reflection.EventInfo</c> type.
			/// </summary>
			public static readonly TypeIdentity EventInfo = TypeIdentity.Create<global::System.Reflection.EventInfo>();

			/// <summary>
			/// The <c>System.Reflection.CustomAttributeData</c> type.
			/// </summary>
			public static readonly TypeIdentity CustomAttributeData =
				TypeIdentity.Create<global::System.Reflection.CustomAttributeData>();

			/// <summary>
			/// The <c>System.Reflection.AssemblyMetadataAttribute</c> type.
			/// </summary>
			public static readonly TypeIdentity AssemblyMetadataAttribute =
				TypeIdentity.Create<global::System.Reflection.AssemblyMetadataAttribute>();

			/// <summary>
			/// The <c>System.Reflection.AssemblyName</c> type.
			/// </summary>
			public static readonly TypeIdentity AssemblyName =
				TypeIdentity.Create<global::System.Reflection.AssemblyName>();
		}

		/// <summary>
		/// Common types from the <c>System.Diagnostics</c> namespace hierarchy.
		/// </summary>
		public static partial class Diagnostics
		{
			/// <summary>
			/// The <c>System.Diagnostics</c> namespace.
			/// </summary>
			public const string Namespace = "System.Diagnostics";

			/// <summary>
			/// The <c>System.Diagnostics.Debugger</c> type.
			/// </summary>
			public static readonly TypeIdentity Debugger = new(nameof(Debugger), Namespace);

			/// <summary>
			/// The <c>System.Diagnostics.Debug</c> type.
			/// </summary>
			public static readonly TypeIdentity Debug = new(nameof(Debug), Namespace);

			/// <summary>
			/// The <c>System.Diagnostics.ConditionalAttribute</c> type.
			/// </summary>
			public static readonly TypeIdentity ConditionalAttribute =
				TypeIdentity.Create<global::System.Diagnostics.ConditionalAttribute>();

			/// <summary>
			/// Common types from the <c>System.Diagnostics.CodeAnalysis</c> namespace.
			/// </summary>
			public static partial class CodeAnalysis
			{
				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis</c> namespace.
				/// </summary>
				public const string Namespace = "System.Diagnostics.CodeAnalysis";

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.SuppressMessageAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity SuppressMessageAttribute = new(
					nameof(SuppressMessageAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity ExcludeFromCodeCoverageAttribute = new(
					nameof(ExcludeFromCodeCoverageAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.AllowNullAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity AllowNullAttribute = new(nameof(AllowNullAttribute), Namespace);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.DisallowNullAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity DisallowNullAttribute = new(
					nameof(DisallowNullAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.MaybeNullAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity MaybeNullAttribute = new(nameof(MaybeNullAttribute), Namespace);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.NotNullAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity NotNullAttribute = new(nameof(NotNullAttribute), Namespace);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity MaybeNullWhenAttribute = new(
					nameof(MaybeNullWhenAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.NotNullWhenAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity NotNullWhenAttribute = new(nameof(NotNullWhenAttribute), Namespace);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity NotNullIfNotNullAttribute = new(
					nameof(NotNullIfNotNullAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.MemberNotNullAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity MemberNotNullAttribute = new(
					nameof(MemberNotNullAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.MemberNotNullWhenAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity MemberNotNullWhenAttribute = new(
					nameof(MemberNotNullWhenAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.DoesNotReturnAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity DoesNotReturnAttribute = new(
					nameof(DoesNotReturnAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.DoesNotReturnIfAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity DoesNotReturnIfAttribute = new(
					nameof(DoesNotReturnIfAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity UnconditionalSuppressMessageAttribute = new(
					nameof(UnconditionalSuppressMessageAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity DynamicallyAccessedMembersAttribute = new(
					nameof(DynamicallyAccessedMembersAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes</c> type.
				/// </summary>
				public static readonly TypeIdentity DynamicallyAccessedMemberTypes = new(
					nameof(DynamicallyAccessedMemberTypes),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity RequiresUnreferencedCodeAttribute = new(
					nameof(RequiresUnreferencedCodeAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity SetsRequiredMembersAttribute = new(
					nameof(SetsRequiredMembersAttribute),
					Namespace
				);
			}
		}

		/// <summary>
		/// Common types from the <c>System.Runtime</c> namespace hierarchy.
		/// </summary>
		public static partial class Runtime
		{
			/// <summary>
			/// The <c>System.Runtime</c> namespace.
			/// </summary>
			public const string Namespace = "System.Runtime";

			/// <summary>
			/// Common types from the <c>System.Runtime.CompilerServices</c> namespace.
			/// </summary>
			public static partial class CompilerServices
			{
				/// <summary>
				/// The <c>System.Runtime.CompilerServices</c> namespace.
				/// </summary>
				public const string Namespace = "System.Runtime.CompilerServices";

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.CompilerGeneratedAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CompilerGeneratedAttribute = new(
					nameof(CompilerGeneratedAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.InternalsVisibleToAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity InternalsVisibleToAttribute = new(
					nameof(InternalsVisibleToAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.MethodImplAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity MethodImplAttribute = new(nameof(MethodImplAttribute), Namespace);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.ExtensionAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity ExtensionAttribute = new(nameof(ExtensionAttribute), Namespace);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.CallerMemberNameAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CallerMemberNameAttribute = new(
					nameof(CallerMemberNameAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.CallerFilePathAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CallerFilePathAttribute = new(
					nameof(CallerFilePathAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.CallerLineNumberAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CallerLineNumberAttribute = new(
					nameof(CallerLineNumberAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.CallerArgumentExpressionAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CallerArgumentExpressionAttribute = new(
					nameof(CallerArgumentExpressionAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.IsExternalInit</c> type.
				/// </summary>
				public static readonly TypeIdentity IsExternalInit = new(nameof(IsExternalInit), Namespace);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.RequiredMemberAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity RequiredMemberAttribute = new(
					nameof(RequiredMemberAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.ModuleInitializerAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity ModuleInitializerAttribute = new(
					nameof(ModuleInitializerAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.SkipLocalsInitAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity SkipLocalsInitAttribute = new(
					nameof(SkipLocalsInitAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.EnumeratorCancellationAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity EnumeratorCancellationAttribute = new(
					nameof(EnumeratorCancellationAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity InterpolatedStringHandlerAttribute = new(
					nameof(InterpolatedStringHandlerAttribute),
					Namespace
				);

				/// <summary>
				/// The
				/// <c>System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute</c>
				/// type.
				/// </summary>
				public static readonly TypeIdentity InterpolatedStringHandlerArgumentAttribute = new(
					nameof(InterpolatedStringHandlerArgumentAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.Runtime.CompilerServices.CollectionBuilderAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CollectionBuilderAttribute = new(
					nameof(CollectionBuilderAttribute),
					Namespace
				);
			}
		}

		/// <summary>
		/// Common types from the <c>System.CodeDom</c> namespace hierarchy.
		/// </summary>
		public static partial class CodeDom
		{
			/// <summary>
			/// The <c>System.CodeDom</c> namespace.
			/// </summary>
			public const string Namespace = "System.CodeDom";

			/// <summary>
			/// Common types from the <c>System.CodeDom.Compiler</c> namespace.
			/// </summary>
			public static partial class Compiler
			{
				/// <summary>
				/// The <c>System.CodeDom.Compiler</c> namespace.
				/// </summary>
				public const string Namespace = "System.CodeDom.Compiler";

				/// <summary>
				/// The <c>System.CodeDom.Compiler.GeneratedCodeAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity GeneratedCodeAttribute = new(
					nameof(GeneratedCodeAttribute),
					Namespace
				);
			}
		}

		/// <summary>
		/// Common types from the <c>System.ComponentModel</c> namespace hierarchy.
		/// </summary>
		public static partial class ComponentModel
		{
			/// <summary>
			/// The <c>System.ComponentModel</c> namespace.
			/// </summary>
			public const string Namespace = "System.ComponentModel";

			/// <summary>
			/// The <c>System.ComponentModel.EditorBrowsableAttribute</c> type.
			/// </summary>
			public static readonly TypeIdentity EditorBrowsableAttribute = new(
				nameof(EditorBrowsableAttribute),
				Namespace
			);

			/// <summary>
			/// The <c>System.ComponentModel.EditorBrowsableState</c> type.
			/// </summary>
			public static readonly TypeIdentity EditorBrowsableState = new(nameof(EditorBrowsableState), Namespace);

			/// <summary>
			/// The <c>System.ComponentModel.DescriptionAttribute</c> type.
			/// </summary>
			public static readonly TypeIdentity DescriptionAttribute = new(nameof(DescriptionAttribute), Namespace);

			/// <summary>
			/// The <c>System.ComponentModel.DefaultValueAttribute</c> type.
			/// </summary>
			public static readonly TypeIdentity DefaultValueAttribute = new(nameof(DefaultValueAttribute), Namespace);

			/// <summary>
			/// Common types from the <c>System.ComponentModel.DataAnnotations</c> namespace.
			/// </summary>
			public static partial class DataAnnotations
			{
				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations</c> namespace.
				/// </summary>
				public const string Namespace = "System.ComponentModel.DataAnnotations";

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.ValidationAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity ValidationAttribute = new(nameof(ValidationAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.RequiredAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity RequiredAttribute = new(nameof(RequiredAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.RangeAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity RangeAttribute = new(nameof(RangeAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.RegularExpressionAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity RegularExpressionAttribute = new(
					nameof(RegularExpressionAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.StringLengthAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity StringLengthAttribute = new(
					nameof(StringLengthAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.MinLengthAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity MinLengthAttribute = new(nameof(MinLengthAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.MaxLengthAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity MaxLengthAttribute = new(nameof(MaxLengthAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.CompareAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CompareAttribute = new(nameof(CompareAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.DisplayAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity DisplayAttribute = new(nameof(DisplayAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.DataTypeAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity DataTypeAttribute = new(nameof(DataTypeAttribute), Namespace);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.EnumDataTypeAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity EnumDataTypeAttribute = new(
					nameof(EnumDataTypeAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.CustomValidationAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity CustomValidationAttribute = new(
					nameof(CustomValidationAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>System.ComponentModel.DataAnnotations.EmailAddressAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity EmailAddressAttribute = new(
					nameof(EmailAddressAttribute),
					Namespace
				);
			}
		}

		/// <summary>
		/// Common types from the <c>System.Text</c> namespace hierarchy.
		/// </summary>
		public static partial class Text
		{
			/// <summary>
			/// The <c>System.Text</c> namespace.
			/// </summary>
			public const string Namespace = "System.Text";

			/// <summary>
			/// The <c>System.Text.Encoding</c> type.
			/// </summary>
			public static readonly TypeIdentity Encoding = new(nameof(Encoding), Namespace);

			/// <summary>
			/// The <c>System.Text.Encoder</c> type.
			/// </summary>
			public static readonly TypeIdentity Encoder = new(nameof(Encoder), Namespace);

			/// <summary>
			/// The <c>System.Text.Decoder</c> type.
			/// </summary>
			public static readonly TypeIdentity Decoder = new(nameof(Decoder), Namespace);

			/// <summary>
			/// The <c>System.Text.StringBuilder</c> type.
			/// </summary>
			public static readonly TypeIdentity StringBuilder = new(nameof(StringBuilder), Namespace);

			/// <summary>
			/// The <c>System.Text.Rune</c> type.
			/// </summary>
			public static readonly TypeIdentity Rune = new(nameof(Rune), Namespace);

			/// <summary>
			/// The <c>System.Text.NormalizationForm</c> type.
			/// </summary>
			public static readonly TypeIdentity NormalizationForm = new(nameof(NormalizationForm), Namespace);

			/// <summary>
			/// The <c>System.Text.UTF8Encoding</c> type.
			/// </summary>
			public static readonly TypeIdentity UTF8Encoding = TypeIdentity.Create<global::System.Text.UTF8Encoding>();

			/// <summary>
			/// The <c>System.Text.UnicodeEncoding</c> type.
			/// </summary>
			public static readonly TypeIdentity UnicodeEncoding =
				TypeIdentity.Create<global::System.Text.UnicodeEncoding>();

			/// <summary>
			/// The <c>System.Text.ASCIIEncoding</c> type.
			/// </summary>
			public static readonly TypeIdentity ASCIIEncoding =
				TypeIdentity.Create<global::System.Text.ASCIIEncoding>();

			/// <summary>
			/// The <c>System.Text.EncoderFallback</c> type.
			/// </summary>
			public static readonly TypeIdentity EncoderFallback = new(nameof(EncoderFallback), Namespace);

			/// <summary>
			/// The <c>System.Text.DecoderFallback</c> type.
			/// </summary>
			public static readonly TypeIdentity DecoderFallback = new(nameof(DecoderFallback), Namespace);

			/// <summary>
			/// The <c>System.Text.EncoderFallbackException</c> type.
			/// </summary>
			public static readonly TypeIdentity EncoderFallbackException = new(
				nameof(EncoderFallbackException),
				Namespace
			);

			/// <summary>
			/// The <c>System.Text.DecoderFallbackException</c> type.
			/// </summary>
			public static readonly TypeIdentity DecoderFallbackException = new(
				nameof(DecoderFallbackException),
				Namespace
			);

			/// <summary>
			/// Common types from the <c>System.Text.RegularExpressions</c> namespace.
			/// </summary>
			public static partial class RegularExpressions
			{
				/// <summary>
				/// The <c>System.Text.RegularExpressions</c> namespace.
				/// </summary>
				public const string Namespace = "System.Text.RegularExpressions";

				/// <summary>
				/// The <c>System.Text.RegularExpressions.Regex</c> type.
				/// </summary>
				public static readonly TypeIdentity Regex =
					TypeIdentity.Create<global::System.Text.RegularExpressions.Regex>();

				/// <summary>
				/// The <c>System.Text.RegularExpressions.RegexOptions</c> type.
				/// </summary>
				public static readonly TypeIdentity RegexOptions =
					TypeIdentity.Create<global::System.Text.RegularExpressions.RegexOptions>();

				/// <summary>
				/// The <c>System.Text.RegularExpressions.Match</c> type.
				/// </summary>
				public static readonly TypeIdentity Match =
					TypeIdentity.Create<global::System.Text.RegularExpressions.Match>();

				/// <summary>
				/// The <c>System.Text.RegularExpressions.Group</c> type.
				/// </summary>
				public static readonly TypeIdentity Group =
					TypeIdentity.Create<global::System.Text.RegularExpressions.Group>();

				/// <summary>
				/// The <c>System.Text.RegularExpressions.Capture</c> type.
				/// </summary>
				public static readonly TypeIdentity Capture =
					TypeIdentity.Create<global::System.Text.RegularExpressions.Capture>();

				/// <summary>
				/// The <c>System.Text.RegularExpressions.MatchCollection</c> type.
				/// </summary>
				public static readonly TypeIdentity MatchCollection =
					TypeIdentity.Create<global::System.Text.RegularExpressions.MatchCollection>();

				/// <summary>
				/// The <c>System.Text.RegularExpressions.GroupCollection</c> type.
				/// </summary>
				public static readonly TypeIdentity GroupCollection =
					TypeIdentity.Create<global::System.Text.RegularExpressions.GroupCollection>();

				/// <summary>
				/// The <c>System.Text.RegularExpressions.CaptureCollection</c> type.
				/// </summary>
				public static readonly TypeIdentity CaptureCollection =
					TypeIdentity.Create<global::System.Text.RegularExpressions.CaptureCollection>();
			}

			/// <summary>
			/// Common types from the <c>System.Text.Json</c> namespace hierarchy.
			/// </summary>
			public static partial class Json
			{
				/// <summary>
				/// The <c>System.Text.Json</c> namespace.
				/// </summary>
				public const string Namespace = "System.Text.Json";

				/// <summary>
				/// The <c>System.Text.Json.JsonSerializer</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonSerializer = new(nameof(JsonSerializer), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonException</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonException = new(nameof(JsonException), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonSerializerOptions</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonSerializerOptions = new(
					nameof(JsonSerializerOptions),
					Namespace
				);

				/// <summary>
				/// The <c>System.Text.Json.JsonSerializerDefaults</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonSerializerDefaults = new(
					nameof(JsonSerializerDefaults),
					Namespace
				);

				/// <summary>
				/// The <c>System.Text.Json.JsonDocument</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonDocument = new(nameof(JsonDocument), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonDocumentOptions</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonDocumentOptions = new(nameof(JsonDocumentOptions), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonElement</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonElement = new(nameof(JsonElement), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonProperty</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonProperty = new(nameof(JsonProperty), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonValueKind</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonValueKind = new(nameof(JsonValueKind), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonTokenType</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonTokenType = new(nameof(JsonTokenType), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonCommentHandling</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonCommentHandling = new(nameof(JsonCommentHandling), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonNamingPolicy</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonNamingPolicy = new(nameof(JsonNamingPolicy), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonReaderOptions</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonReaderOptions = new(nameof(JsonReaderOptions), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonWriterOptions</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonWriterOptions = new(nameof(JsonWriterOptions), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.Utf8JsonReader</c> type.
				/// </summary>
				public static readonly TypeIdentity Utf8JsonReader = new(nameof(Utf8JsonReader), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.Utf8JsonWriter</c> type.
				/// </summary>
				public static readonly TypeIdentity Utf8JsonWriter = new(nameof(Utf8JsonWriter), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonEncodedText</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonEncodedText = new(nameof(JsonEncodedText), Namespace);

				/// <summary>
				/// The <c>System.Text.Json.JsonReaderState</c> type.
				/// </summary>
				public static readonly TypeIdentity JsonReaderState = new(nameof(JsonReaderState), Namespace);

				/// <summary>
				/// Common types from the <c>System.Text.Json.Nodes</c> namespace.
				/// </summary>
				public static partial class Nodes
				{
					/// <summary>
					/// The <c>System.Text.Json.Nodes</c> namespace.
					/// </summary>
					public const string Namespace = "System.Text.Json.Nodes";

					/// <summary>
					/// The <c>System.Text.Json.Nodes.JsonNode</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonNode = new(nameof(JsonNode), Namespace);

					/// <summary>
					/// The <c>System.Text.Json.Nodes.JsonObject</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonObject = new(nameof(JsonObject), Namespace);

					/// <summary>
					/// The <c>System.Text.Json.Nodes.JsonArray</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonArray = new(nameof(JsonArray), Namespace);

					/// <summary>
					/// The <c>System.Text.Json.Nodes.JsonValue</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonValue = new(nameof(JsonValue), Namespace);

					/// <summary>
					/// The <c>System.Text.Json.Nodes.JsonNodeOptions</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonNodeOptions = new(nameof(JsonNodeOptions), Namespace);
				}

				/// <summary>
				/// Common types from the <c>System.Text.Json.Serialization</c> namespace hierarchy.
				/// </summary>
				public static partial class Serialization
				{
					/// <summary>
					/// The <c>System.Text.Json.Serialization</c> namespace.
					/// </summary>
					public const string Namespace = "System.Text.Json.Serialization";

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonIgnoreAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonIgnoreAttribute = new(
						nameof(JsonIgnoreAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonPropertyNameAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonPropertyNameAttribute = new(
						nameof(JsonPropertyNameAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonIncludeAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonIncludeAttribute = new(
						nameof(JsonIncludeAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonConstructorAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonConstructorAttribute = new(
						nameof(JsonConstructorAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonExtensionDataAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonExtensionDataAttribute = new(
						nameof(JsonExtensionDataAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonNumberHandlingAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonNumberHandlingAttribute = new(
						nameof(JsonNumberHandlingAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonConverterAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonConverterAttribute = new(
						nameof(JsonConverterAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonRequiredAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonRequiredAttribute = new(
						nameof(JsonRequiredAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonDerivedTypeAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonDerivedTypeAttribute = new(
						nameof(JsonDerivedTypeAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonPolymorphicAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonPolymorphicAttribute = new(
						nameof(JsonPolymorphicAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonPropertyOrderAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonPropertyOrderAttribute = new(
						nameof(JsonPropertyOrderAttribute),
						Namespace
					);

					/// <summary>
					/// The
					/// <c>System.Text.Json.Serialization.JsonUnmappedMemberHandlingAttribute</c>
					/// type.
					/// </summary>
					public static readonly TypeIdentity JsonUnmappedMemberHandlingAttribute = new(
						nameof(JsonUnmappedMemberHandlingAttribute),
						Namespace
					);

					/// <summary>
					/// The
					/// <c>System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute</c>
					/// type.
					/// </summary>
					public static readonly TypeIdentity JsonObjectCreationHandlingAttribute = new(
						nameof(JsonObjectCreationHandlingAttribute),
						Namespace
					);

					/// <summary>
					/// The
					/// <c>System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute</c>
					/// type.
					/// </summary>
					public static readonly TypeIdentity JsonStringEnumMemberNameAttribute = new(
						nameof(JsonStringEnumMemberNameAttribute),
						Namespace
					);

					/// <summary>
					/// The non-generic <c>System.Text.Json.Serialization.JsonConverter</c> type.
					/// </summary>
					/// <remarks>
					/// The generic <c>System.Text.Json.Serialization.JsonConverter{T}</c> type
					/// has arity 1 and can be represented by applying that arity to this identity.
					/// </remarks>
					public static readonly TypeIdentity JsonConverter = new(nameof(JsonConverter), Namespace, 1);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonConverterFactory</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonConverterFactory = new(
						nameof(JsonConverterFactory),
						Namespace,
						1
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonStringEnumConverter</c> type.
					/// The generic <c>JsonStringEnumConverter{TEnum}</c> form has arity 1.
					/// </summary>
					public static readonly TypeIdentity JsonStringEnumConverter = new(
						nameof(JsonStringEnumConverter),
						Namespace,
						1
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonIgnoreCondition</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonIgnoreCondition = new(
						nameof(JsonIgnoreCondition),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonNumberHandling</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonNumberHandling = new(nameof(JsonNumberHandling), Namespace);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonKnownNamingPolicy</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonKnownNamingPolicy = new(
						nameof(JsonKnownNamingPolicy),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonUnknownTypeHandling</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonUnknownTypeHandling = new(
						nameof(JsonUnknownTypeHandling),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonUnmappedMemberHandling</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonUnmappedMemberHandling = new(
						nameof(JsonUnmappedMemberHandling),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonUnknownDerivedTypeHandling</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonUnknownDerivedTypeHandling = new(
						nameof(JsonUnknownDerivedTypeHandling),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonObjectCreationHandling</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonObjectCreationHandling = new(
						nameof(JsonObjectCreationHandling),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.ReferenceHandler</c> type.
					/// </summary>
					public static readonly TypeIdentity ReferenceHandler = new(nameof(ReferenceHandler), Namespace);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.ReferenceResolver</c> type.
					/// </summary>
					public static readonly TypeIdentity ReferenceResolver = new(nameof(ReferenceResolver), Namespace);

					/// <summary>
					/// The
					/// <c>System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute</c>
					/// type.
					/// </summary>
					public static readonly TypeIdentity JsonSourceGenerationOptionsAttribute = new(
						nameof(JsonSourceGenerationOptionsAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonSerializableAttribute</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonSerializableAttribute = new(
						nameof(JsonSerializableAttribute),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonSourceGenerationMode</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonSourceGenerationMode = new(
						nameof(JsonSourceGenerationMode),
						Namespace
					);

					/// <summary>
					/// The <c>System.Text.Json.Serialization.JsonSerializerContext</c> type.
					/// </summary>
					public static readonly TypeIdentity JsonSerializerContext = new(
						nameof(JsonSerializerContext),
						Namespace
					);

					/// <summary>
					/// Common types from the
					/// <c>System.Text.Json.Serialization.Metadata</c> namespace.
					/// </summary>
					public static partial class Metadata
					{
						/// <summary>
						/// The <c>System.Text.Json.Serialization.Metadata</c> namespace.
						/// </summary>
						public const string Namespace = "System.Text.Json.Serialization.Metadata";

						/// <summary>
						/// The non-generic
						/// <c>System.Text.Json.Serialization.Metadata.JsonTypeInfo</c> type.
						/// </summary>
						/// <remarks>
						/// The generic
						/// <c>System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}</c>
						/// type has arity 1 and can be represented by applying that arity
						/// to this identity.
						/// </remarks>
						public static readonly TypeIdentity JsonTypeInfo = new(nameof(JsonTypeInfo), Namespace, 1);

						/// <summary>
						/// The <c>System.Text.Json.Serialization.Metadata.JsonPropertyInfo</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonPropertyInfo = new(nameof(JsonPropertyInfo), Namespace);

						/// <summary>
						/// The <c>System.Text.Json.Serialization.Metadata.JsonParameterInfo</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonParameterInfo = new(
							nameof(JsonParameterInfo),
							Namespace
						);

						/// <summary>
						/// The <c>System.Text.Json.Serialization.Metadata.JsonTypeInfoKind</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonTypeInfoKind = new(nameof(JsonTypeInfoKind), Namespace);

						/// <summary>
						/// The
						/// <c>System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity IJsonTypeInfoResolver = new(
							nameof(IJsonTypeInfoResolver),
							Namespace
						);

						/// <summary>
						/// The
						/// <c>System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity DefaultJsonTypeInfoResolver = new(
							nameof(DefaultJsonTypeInfoResolver),
							Namespace
						);

						/// <summary>
						/// The
						/// <c>System.Text.Json.Serialization.Metadata.JsonPolymorphismOptions</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonPolymorphismOptions = new(
							nameof(JsonPolymorphismOptions),
							Namespace
						);

						/// <summary>
						/// The
						/// <c>System.Text.Json.Serialization.Metadata.JsonTypeInfoResolver</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonTypeInfoResolver = new(
							nameof(JsonTypeInfoResolver),
							Namespace
						);

						/// <summary>
						/// The <c>System.Text.Json.Serialization.Metadata.JsonDerivedType</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonDerivedType = new(nameof(JsonDerivedType), Namespace);

						/// <summary>
						/// The
						/// <c>System.Text.Json.Serialization.Metadata.JsonMetadataServices</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonMetadataServices = new(
							nameof(JsonMetadataServices),
							Namespace
						);

						/// <summary>
						/// The generic
						/// <c>System.Text.Json.Serialization.Metadata.JsonObjectInfoValues{T}</c>
						/// type. The type has arity 1.
						/// </summary>
						public static readonly TypeIdentity JsonObjectInfoValues = new(
							nameof(JsonObjectInfoValues),
							Namespace
						);

						/// <summary>
						/// The generic
						/// <c>System.Text.Json.Serialization.Metadata.JsonCollectionInfoValues{TCollection}</c>
						/// type. The type has arity 1.
						/// </summary>
						public static readonly TypeIdentity JsonCollectionInfoValues = new(
							nameof(JsonCollectionInfoValues),
							Namespace
						);

						/// <summary>
						/// The generic
						/// <c>System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues{T}</c>
						/// type. The type has arity 1.
						/// </summary>
						public static readonly TypeIdentity JsonPropertyInfoValues = new(
							nameof(JsonPropertyInfoValues),
							Namespace
						);

						/// <summary>
						/// The
						/// <c>System.Text.Json.Serialization.Metadata.JsonParameterInfoValues</c>
						/// type.
						/// </summary>
						public static readonly TypeIdentity JsonParameterInfoValues = new(
							nameof(JsonParameterInfoValues),
							Namespace
						);
					}
				}
			}
		}
	}

	/// <summary>
	/// Common types from the <c>Microsoft</c> namespace hierarchy.
	/// </summary>
	public static class Microsoft
	{
		/// <summary>
		/// The <c>Microsoft</c> namespace.
		/// </summary>
		public const string Namespace = "Microsoft";

		/// <summary>
		/// Common types from the <c>Microsoft.CodeAnalysis</c> namespace hierarchy.
		/// </summary>
		public static class CodeAnalysis
		{
			/// <summary>
			/// The <c>Microsoft.CodeAnalysis</c> namespace.
			/// </summary>
			public const string Namespace = "Microsoft.CodeAnalysis";

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.EmbeddedAttribute</c> type.
			/// </summary>
			public static readonly TypeIdentity EmbeddedAttribute = new(nameof(EmbeddedAttribute), Namespace);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.GeneratorAttribute</c> type.
			/// </summary>
			public static readonly TypeIdentity GeneratorAttribute = new(
				nameof(global::Microsoft.CodeAnalysis.GeneratorAttribute),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.ISourceGenerator</c> type.
			/// </summary>
			public static readonly TypeIdentity ISourceGenerator = new(
				nameof(global::Microsoft.CodeAnalysis.ISourceGenerator),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.IIncrementalGenerator</c> type.
			/// </summary>
			public static readonly TypeIdentity IIncrementalGenerator = new(
				nameof(global::Microsoft.CodeAnalysis.IIncrementalGenerator),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.GeneratorInitializationContext</c> type.
			/// </summary>
			public static readonly TypeIdentity GeneratorInitializationContext = new(
				nameof(global::Microsoft.CodeAnalysis.GeneratorInitializationContext),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.GeneratorExecutionContext</c> type.
			/// </summary>
			public static readonly TypeIdentity GeneratorExecutionContext = new(
				nameof(global::Microsoft.CodeAnalysis.GeneratorExecutionContext),
				Namespace
			);

			/// <summary>
			/// The
			/// <c>Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext</c>
			/// type.
			/// </summary>
			public static readonly TypeIdentity IncrementalGeneratorInitializationContext = new(
				nameof(global::Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.SourceProductionContext</c> type.
			/// </summary>
			public static readonly TypeIdentity SourceProductionContext = new(
				nameof(global::Microsoft.CodeAnalysis.SourceProductionContext),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.Compilation</c> type.
			/// </summary>
			public static readonly TypeIdentity Compilation = new(
				nameof(global::Microsoft.CodeAnalysis.Compilation),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.SyntaxTree</c> type.
			/// </summary>
			public static readonly TypeIdentity SyntaxTree = new(
				nameof(global::Microsoft.CodeAnalysis.SyntaxTree),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.SyntaxNode</c> type.
			/// </summary>
			public static readonly TypeIdentity SyntaxNode = new(
				nameof(global::Microsoft.CodeAnalysis.SyntaxNode),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.SemanticModel</c> type.
			/// </summary>
			public static readonly TypeIdentity SemanticModel = new(
				nameof(global::Microsoft.CodeAnalysis.SemanticModel),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.ISymbol</c> type.
			/// </summary>
			public static readonly TypeIdentity ISymbol = new(
				nameof(global::Microsoft.CodeAnalysis.ISymbol),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.INamedTypeSymbol</c> type.
			/// </summary>
			public static readonly TypeIdentity INamedTypeSymbol = new(
				nameof(global::Microsoft.CodeAnalysis.INamedTypeSymbol),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.ITypeSymbol</c> type.
			/// </summary>
			public static readonly TypeIdentity ITypeSymbol = new(
				nameof(global::Microsoft.CodeAnalysis.ITypeSymbol),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.IMethodSymbol</c> type.
			/// </summary>
			public static readonly TypeIdentity IMethodSymbol = new(
				nameof(global::Microsoft.CodeAnalysis.IMethodSymbol),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.IPropertySymbol</c> type.
			/// </summary>
			public static readonly TypeIdentity IPropertySymbol = new(
				nameof(global::Microsoft.CodeAnalysis.IPropertySymbol),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.IFieldSymbol</c> type.
			/// </summary>
			public static readonly TypeIdentity IFieldSymbol = new(
				nameof(global::Microsoft.CodeAnalysis.IFieldSymbol),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.AttributeData</c> type.
			/// </summary>
			public static readonly TypeIdentity AttributeData = new(
				nameof(global::Microsoft.CodeAnalysis.AttributeData),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.TypedConstant</c> type.
			/// </summary>
			public static readonly TypeIdentity TypedConstant = new(
				nameof(global::Microsoft.CodeAnalysis.TypedConstant),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.Diagnostic</c> type.
			/// </summary>
			public static readonly TypeIdentity Diagnostic = new(
				nameof(global::Microsoft.CodeAnalysis.Diagnostic),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.DiagnosticDescriptor</c> type.
			/// </summary>
			public static readonly TypeIdentity DiagnosticDescriptor = new(
				nameof(global::Microsoft.CodeAnalysis.DiagnosticDescriptor),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.DiagnosticSeverity</c> type.
			/// </summary>
			public static readonly TypeIdentity DiagnosticSeverity = new(
				nameof(global::Microsoft.CodeAnalysis.DiagnosticSeverity),
				Namespace
			);

			/// <summary>
			/// The <c>Microsoft.CodeAnalysis.Location</c> type.
			/// </summary>
			public static readonly TypeIdentity Location = new(
				nameof(global::Microsoft.CodeAnalysis.Location),
				Namespace
			);

			/// <summary>
			/// Common types from the <c>Microsoft.CodeAnalysis.Diagnostics</c> namespace.
			/// </summary>
			public static class Diagnostics
			{
				/// <summary>
				/// The <c>Microsoft.CodeAnalysis.Diagnostics</c> namespace.
				/// </summary>
				public const string Namespace = "Microsoft.CodeAnalysis.Diagnostics";

				/// <summary>
				/// The <c>Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer</c> type.
				/// </summary>
				public static readonly TypeIdentity DiagnosticAnalyzer = new(
					nameof(global::Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer),
					Namespace
				);

				/// <summary>
				/// The
				/// <c>Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute</c>
				/// type.
				/// </summary>
				public static readonly TypeIdentity DiagnosticAnalyzerAttribute = new(
					nameof(global::Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.CodeAnalysis.Diagnostics.AnalysisContext</c> type.
				/// </summary>
				public static readonly TypeIdentity AnalysisContext = new(
					nameof(global::Microsoft.CodeAnalysis.Diagnostics.AnalysisContext),
					Namespace
				);
			}

			/// <summary>
			/// Common types from the <c>Microsoft.CodeAnalysis.CSharp</c> namespace.
			/// </summary>
			public static class CSharp
			{
				/// <summary>
				/// The <c>Microsoft.CodeAnalysis.CSharp</c> namespace.
				/// </summary>
				public const string Namespace = "Microsoft.CodeAnalysis.CSharp";

				/// <summary>
				/// The <c>Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree</c> type.
				/// </summary>
				public static readonly TypeIdentity CSharpSyntaxTree = new(
					nameof(global::Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.CodeAnalysis.CSharp.CSharpCompilation</c> type.
				/// </summary>
				public static readonly TypeIdentity CSharpCompilation = new(
					nameof(global::Microsoft.CodeAnalysis.CSharp.CSharpCompilation),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.CodeAnalysis.CSharp.SyntaxKind</c> type.
				/// </summary>
				public static readonly TypeIdentity SyntaxKind = new(
					nameof(global::Microsoft.CodeAnalysis.CSharp.SyntaxKind),
					Namespace
				);
			}
		}

		/// <summary>
		/// Common types from the <c>Microsoft.Extensions</c> namespace hierarchy.
		/// </summary>
		public static partial class Extensions
		{
			/// <summary>
			/// The <c>Microsoft.Extensions</c> namespace.
			/// </summary>
			public const string Namespace = "Microsoft.Extensions";

			/// <summary>
			/// Common types from the <c>Microsoft.Extensions.DependencyInjection</c> namespace.
			/// </summary>
			public static class DependencyInjection
			{
				/// <summary>
				/// The <c>Microsoft.Extensions.DependencyInjection</c> namespace.
				/// </summary>
				public const string Namespace = "Microsoft.Extensions.DependencyInjection";

				/// <summary>
				/// The <c>Microsoft.Extensions.DependencyInjection.IServiceCollection</c> type.
				/// </summary>
				public static readonly TypeIdentity IServiceCollection = new(nameof(IServiceCollection), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.DependencyInjection.ServiceDescriptor</c> type.
				/// </summary>
				public static readonly TypeIdentity ServiceDescriptor = new(nameof(ServiceDescriptor), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.DependencyInjection.ServiceLifetime</c> type.
				/// </summary>
				public static readonly TypeIdentity ServiceLifetime = new(nameof(ServiceLifetime), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.DependencyInjection.ServiceCollection</c> type.
				/// </summary>
				public static readonly TypeIdentity ServiceCollection = new(nameof(ServiceCollection), Namespace);

				/// <summary>
				/// The
				/// <c>Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions</c>
				/// type.
				/// </summary>
				public static readonly TypeIdentity ServiceCollectionServiceExtensions = new(
					nameof(ServiceCollectionServiceExtensions),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.Extensions.DependencyInjection.ActivatorUtilities</c> type.
				/// </summary>
				public static readonly TypeIdentity ActivatorUtilities = new(nameof(ActivatorUtilities), Namespace);

				/// <summary>
				/// The
				/// <c>Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions</c>
				/// type.
				/// </summary>
				public static readonly TypeIdentity ServiceProviderServiceExtensions = new(
					nameof(ServiceProviderServiceExtensions),
					Namespace
				);
			}

			/// <summary>
			/// Common types from the <c>Microsoft.Extensions.Options</c> namespace.
			/// </summary>
			public static class Options
			{
				/// <summary>
				/// The <c>Microsoft.Extensions.Options</c> namespace.
				/// </summary>
				public const string Namespace = "Microsoft.Extensions.Options";

				/// <summary>
				/// The generic <c>Microsoft.Extensions.Options.IOptions{TOptions}</c> type.
				/// The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IOptions = new(nameof(IOptions), Namespace, 1);

				/// <summary>
				/// The generic <c>Microsoft.Extensions.Options.IOptionsSnapshot{TOptions}</c>
				/// type. The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IOptionsSnapshot = new(nameof(IOptionsSnapshot), Namespace, 1);

				/// <summary>
				/// The generic <c>Microsoft.Extensions.Options.IOptionsMonitor{TOptions}</c>
				/// type. The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IOptionsMonitor = new(nameof(IOptionsMonitor), Namespace, 1);

				/// <summary>
				/// The generic <c>Microsoft.Extensions.Options.IValidateOptions{TOptions}</c>
				/// type. The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IValidateOptions = new(nameof(IValidateOptions), Namespace, 1);

				/// <summary>
				/// The generic <c>Microsoft.Extensions.Options.IConfigureOptions{TOptions}</c>
				/// type. The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IConfigureOptions = new(nameof(IConfigureOptions), Namespace, 1);

				/// <summary>
				/// The generic
				/// <c>Microsoft.Extensions.Options.IPostConfigureOptions{TOptions}</c>
				/// type. The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity IPostConfigureOptions = new(
					nameof(IPostConfigureOptions),
					Namespace,
					1
				);

				/// <summary>
				/// The generic <c>Microsoft.Extensions.Options.OptionsBuilder{TOptions}</c>
				/// type. The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity OptionsBuilder = new(nameof(OptionsBuilder), Namespace, 1);

				/// <summary>
				/// The generic <c>Microsoft.Extensions.Options.OptionsFactory{TOptions}</c>
				/// type. The type has arity 1.
				/// </summary>
				public static readonly TypeIdentity OptionsFactory = new(nameof(OptionsFactory), Namespace, 1);

				/// <summary>
				/// The <c>Microsoft.Extensions.Options.Options</c> type.
				/// </summary>
				/// <remarks>
				/// Named <c>OptionsType</c> to avoid conflicting with the containing
				/// <c>Options</c> type-library class.
				/// </remarks>
				public static readonly TypeIdentity OptionsType = new(nameof(Options), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.Options.ValidateOptionsResult</c> type.
				/// </summary>
				public static readonly TypeIdentity ValidateOptionsResult = new(
					nameof(ValidateOptionsResult),
					Namespace
				);
			}

			/// <summary>
			/// Common types from the <c>Microsoft.Extensions.Logging</c> namespace.
			/// </summary>
			public static class Logging
			{
				/// <summary>
				/// The <c>Microsoft.Extensions.Logging</c> namespace.
				/// </summary>
				public const string Namespace = "Microsoft.Extensions.Logging";

				/// <summary>
				/// The <c>Microsoft.Extensions.Logging.ILogger</c> type.
				/// The generic <c>ILogger{TCategoryName}</c> form has arity 1.
				/// </summary>
				public static readonly TypeIdentity ILogger = new(nameof(ILogger), Namespace, 1);

				/// <summary>
				/// The <c>Microsoft.Extensions.Logging.ILoggerFactory</c> type.
				/// </summary>
				public static readonly TypeIdentity ILoggerFactory = new(nameof(ILoggerFactory), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.Logging.ILoggerProvider</c> type.
				/// </summary>
				public static readonly TypeIdentity ILoggerProvider = new(nameof(ILoggerProvider), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.Logging.EventId</c> type.
				/// </summary>
				public static readonly TypeIdentity EventId = new(nameof(EventId), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.Logging.LoggerMessage</c> type.
				/// </summary>
				public static readonly TypeIdentity LoggerMessage = new(nameof(LoggerMessage), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.Logging.LoggerMessageAttribute</c> type.
				/// </summary>
				public static readonly TypeIdentity LoggerMessageAttribute = new(
					nameof(LoggerMessageAttribute),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.Extensions.Logging.LogLevel</c> type.
				/// </summary>
				public static readonly TypeIdentity LogLevel = new(nameof(LogLevel), Namespace);
			}

			/// <summary>
			/// Common types from the <c>Microsoft.Extensions.Configuration</c> namespace.
			/// </summary>
			public static class Configuration
			{
				/// <summary>
				/// The <c>Microsoft.Extensions.Configuration</c> namespace.
				/// </summary>
				public const string Namespace = "Microsoft.Extensions.Configuration";

				/// <summary>
				/// The <c>Microsoft.Extensions.Configuration.IConfiguration</c> type.
				/// </summary>
				public static readonly TypeIdentity IConfiguration = new(nameof(IConfiguration), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.Configuration.IConfigurationSection</c> type.
				/// </summary>
				public static readonly TypeIdentity IConfigurationSection = new(
					nameof(IConfigurationSection),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.Extensions.Configuration.IConfigurationBuilder</c> type.
				/// </summary>
				public static readonly TypeIdentity IConfigurationBuilder = new(
					nameof(IConfigurationBuilder),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.Extensions.Configuration.IConfigurationProvider</c> type.
				/// </summary>
				public static readonly TypeIdentity IConfigurationProvider = new(
					nameof(IConfigurationProvider),
					Namespace
				);

				/// <summary>
				/// The <c>Microsoft.Extensions.Configuration.ConfigurationBuilder</c> type.
				/// </summary>
				public static readonly TypeIdentity ConfigurationBuilder = new(nameof(ConfigurationBuilder), Namespace);

				/// <summary>
				/// The <c>Microsoft.Extensions.Configuration.IConfigurationRoot</c> type.
				/// </summary>
				public static readonly TypeIdentity IConfigurationRoot = new(nameof(IConfigurationRoot), Namespace);
			}
		}
	}
}
