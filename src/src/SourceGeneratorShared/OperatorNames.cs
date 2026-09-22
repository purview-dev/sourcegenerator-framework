namespace Purview.SourceGeneratorFramework;

/// <summary>
/// Contains the names of the operators as defined in the C# language specification. These names are used for operator
/// overloading and are recognized by the C# compiler.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1034:Nested types should not be visible",
	Justification = "<Pending>"
)]
public static class OperatorNames
{
	/// <summary>
	/// Contains the names of the unary operators as defined in the C# language specification. These names are used for
	/// operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class Unary
	{
		/// <summary>
		/// The name of the unary plus operator, which is used to indicate a positive value. In C#, this operator does not
		/// change the value of its operand, but it can be overloaded in user-defined types.
		/// </summary>
		public const string UnaryPlusOperatorName = "op_UnaryPlus";

		/// <summary>
		/// The name of the unary negation operator, which is used to negate a value. In C#, this operator changes the sign of
		/// its operand and can be overloaded in user-defined types.
		/// </summary>
		public const string UnaryNegationOperatorName = "op_UnaryNegation";

		/// <summary>
		/// The name of the checked unary negation operator, which is used to negate a value with overflow checking. In C#,
		/// this operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedUnaryNegationOperatorName = "op_CheckedUnaryNegation";

		/// <summary>
		/// The name of the logical NOT operator, which is used to invert a boolean value. In C#, this operator changes true to
		/// false and false to true, and it can be overloaded in user-defined types.
		/// </summary>
		public const string LogicalNotOperatorName = "op_LogicalNot";

		/// <summary>
		/// The name of the bitwise NOT operator, which is used to invert the bits of an integer value. In C#, this operator
		/// can be overloaded in user-defined types.
		/// </summary>
		public const string OnesComplementOperatorName = "op_OnesComplement";

		/// <summary>
		/// The name of the increment operator, which is used to increase a value by one. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string IncrementOperatorName = "op_Increment";

		/// <summary>
		/// The name of the checked increment operator, which is used to increase a value by one with overflow checking. In C#,
		/// this operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedIncrementOperatorName = "op_CheckedIncrement";

		/// <summary>
		/// The name of the decrement operator, which is used to decrease a value by one. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string DecrementOperatorName = "op_Decrement";

		/// <summary>
		/// The name of the checked decrement operator, which is used to decrease a value by one with overflow checking. In C#,
		/// this operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedDecrementOperatorName = "op_CheckedDecrement";

		/// <summary>
		/// The name of the true operator, which is used to determine if a value is true. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string TrueOperatorName = "op_True";

		/// <summary>
		/// The name of the false operator, which is used to determine if a value is false. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string FalseOperatorName = "op_False";
	}

	/// <summary>
	/// Contains the names of the arithmetic operators as defined in the C# language specification. These names are used
	/// for operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class Arithmetic
	{
		/// <summary>
		/// The name of the addition operator, which is used to add two values. In C#, this operator can be overloaded in
		/// user-defined types.
		/// </summary>
		public const string AdditionOperatorName = "op_Addition";

		/// <summary>
		/// The name of the checked addition operator, which is used to add two values with overflow checking. In C#, this
		/// operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedAdditionOperatorName = "op_CheckedAddition";

		/// <summary>
		/// The name of the subtraction operator, which is used to subtract one value from another. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string SubtractionOperatorName = "op_Subtraction";

		/// <summary>
		/// The name of the checked subtraction operator, which is used to subtract one value from another with overflow
		/// checking. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedSubtractionOperatorName = "op_CheckedSubtraction";

		/// <summary>
		/// The name of the multiplication operator, which is used to multiply two values. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string MultiplyOperatorName = "op_Multiply";

		/// <summary>
		/// The name of the checked multiplication operator, which is used to multiply two values with overflow checking. In
		/// C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedMultiplyOperatorName = "op_CheckedMultiply";

		/// <summary>
		/// The name of the division operator, which is used to divide one value by another. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string DivisionOperatorName = "op_Division";

		/// <summary>
		/// The name of the checked division operator, which is used to divide one value by another with overflow checking. In
		/// C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedDivisionOperatorName = "op_CheckedDivision";

		/// <summary>
		/// The name of the modulus operator, which is used to find the remainder of a division operation. In C#, this operator
		/// can be overloaded in user-defined types.
		/// </summary>
		public const string ModulusOperatorName = "op_Modulus";
	}

	/// <summary>
	/// Contains the names of the bitwise operators as defined in the C# language specification. These names are used for
	/// operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class Bitwise
	{
		/// <summary>
		/// The name of the bitwise AND operator, which is used to perform a logical AND operation on each pair of bits in two
		/// values. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string BitwiseAndOperatorName = "op_BitwiseAnd";

		/// <summary>
		/// The name of the bitwise OR operator, which is used to perform a logical OR operation on each pair of bits in two
		/// values. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string BitwiseOrOperatorName = "op_BitwiseOr";

		/// <summary>
		/// The name of the exclusive OR operator, which is used to perform a logical XOR operation on each pair of bits in two
		/// values. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string ExclusiveOrOperatorName = "op_ExclusiveOr";
	}

	/// <summary>
	/// Contains the names of the shift operators as defined in the C# language specification. These names are used for
	/// operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class Shift
	{
		/// <summary>
		/// The name of the left shift operator, which is used to shift the bits of a value to the left by a specified number
		/// of positions. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string LeftShiftOperatorName = "op_LeftShift";

		/// <summary>
		/// The name of the right shift operator, which is used to shift the bits of a value to the right by a specified number
		/// of positions. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string RightShiftOperatorName = "op_RightShift";

		/// <summary>
		/// The name of the unsigned right shift operator, which is used to shift the bits of a value to the right by a
		/// specified number of positions, filling the leftmost bits with zeros. In C#, this operator can be overloaded in
		/// user-defined types.
		/// </summary>
		public const string UnsignedRightShiftOperatorName = "op_UnsignedRightShift";
	}

	/// <summary>
	/// Contains the names of the equality and relational operators as defined in the C# language specification. These
	/// names are used for operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class EqualityAndRelational
	{
		/// <summary>
		/// The name of the equality operator, which is used to compare two values for equality. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string EqualityOperatorName = "op_Equality";

		/// <summary>
		/// The name of the inequality operator, which is used to compare two values for inequality. In C#, this operator can
		/// be overloaded in user-defined types.
		/// </summary>
		public const string InequalityOperatorName = "op_Inequality";

		/// <summary>
		/// The name of the less than operator, which is used to compare two values to determine if the first is less than the
		/// second. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string LessThanOperatorName = "op_LessThan";

		/// <summary>
		/// The name of the greater than operator, which is used to compare two values to determine if the first is greater
		/// than the second. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string GreaterThanOperatorName = "op_GreaterThan";

		/// <summary>
		/// The name of the less than or equal to operator, which is used to compare two values to determine if the first is
		/// less than or equal to the second. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string LessThanOrEqualOperatorName = "op_LessThanOrEqual";

		/// <summary>
		/// The name of the greater than or equal to operator, which is used to compare two values to determine if the first is
		/// greater than or equal to the second. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string GreaterThanOrEqualOperatorName = "op_GreaterThanOrEqual";
	}

	/// <summary>
	/// Contains the names of the conversion operators as defined in the C# language specification. These names are used
	/// for operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class Conversion
	{
		/// <summary>
		/// The name of the implicit conversion operator, which is used to define a conversion from one type to another that
		/// can be performed implicitly. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string ImplicitOperatorName = "op_Implicit";

		/// <summary>
		/// The name of the explicit conversion operator, which is used to define a conversion from one type to another that
		/// can be performed explicitly. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string ExplicitOperatorName = "op_Explicit";

		/// <summary>
		/// The name of the checked explicit conversion operator, which is used to define a conversion from one type to another
		/// that can be performed explicitly and will throw an exception if the conversion results in an overflow. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string CheckedExplicitOperatorName = "op_CheckedExplicit";
	}

	/// <summary>
	/// Contains the names of the compound assignment and reserved operators as defined in the C# language specification.
	/// These names are used for operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class CompoundAndReserved
	{
		/// <summary>
		/// The name of the addition assignment operator, which is used to add a value to a variable and assign the result back
		/// to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string AdditionAssignmentOperatorName = "op_AdditionAssignment";

		/// <summary>
		/// The name of the subtraction assignment operator, which is used to subtract a value from a variable and assign the
		/// result back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string SubtractionAssignmentOperatorName = "op_SubtractionAssignment";

		/// <summary>
		/// The name of the multiplication assignment operator, which is used to multiply a variable by a value and assign the
		/// result back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string MultiplicationAssignmentOperatorName = "op_MultiplicationAssignment";

		/// <summary>
		/// The name of the division assignment operator, which is used to divide a variable by a value and assign the result
		/// back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string DivisionAssignmentOperatorName = "op_DivisionAssignment";

		/// <summary>
		/// The name of the modulus assignment operator, which is used to divide a variable by a value and assign the remainder
		/// back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string ModulusAssignmentOperatorName = "op_ModulusAssignment";

		/// <summary>
		/// The name of the bitwise AND assignment operator, which is used to perform a bitwise AND operation on a variable
		/// and assign the result back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string BitwiseAndAssignmentOperatorName = "op_BitwiseAndAssignment";

		/// <summary>
		/// The name of the bitwise OR assignment operator, which is used to perform a bitwise OR operation on a variable and
		/// assign the result back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string BitwiseOrAssignmentOperatorName = "op_BitwiseOrAssignment";

		/// <summary>
		/// The name of the exclusive OR assignment operator, which is used to perform a bitwise XOR operation on a variable
		/// and assign the result back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string ExclusiveOrAssignmentOperatorName = "op_ExclusiveOrAssignment";

		/// <summary>
		/// The name of the left shift assignment operator, which is used to perform a left shift operation on a variable and
		/// assign the result back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string LeftShiftAssignmentOperatorName = "op_LeftShiftAssignment";

		/// <summary>
		/// The name of the right shift assignment operator, which is used to perform a right shift operation on a variable and
		/// assign the result back to the variable. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string RightShiftAssignmentOperatorName = "op_RightShiftAssignment";

		/// <summary>
		/// The name of the unsigned right shift assignment operator, which is used to perform an unsigned right shift
		/// operation on a variable and assign the result back to the variable. In C#, this operator can be overloaded in
		/// user-defined types.
		/// </summary>
		public const string UnsignedRightShiftAssignmentOperatorName = "op_UnsignedRightShiftAssignment";
	}

	/// <summary>
	/// Contains the names of the reserved operators as defined in the C# language specification. These names are used for
	/// operator overloading and are recognized by the C# compiler.
	/// </summary>
	public static class Reserved
	{
		/// <summary>
		/// The name of the address-of operator, which is used to obtain the memory address of a variable. In C#, this operator
		/// can be overloaded in user-defined types.
		/// </summary>
		public const string AddressOfOperatorName = "op_AddressOf";

		/// <summary>
		/// The name of the assign operator, which is used to assign a value to a variable. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string AssignOperatorName = "op_Assign";

		/// <summary>
		/// The name of the comma operator, which is used to separate expressions in a statement. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string CommaOperatorName = "op_Comma";

		/// <summary>
		/// The name of the conditional operator, which is used to evaluate a condition and return one of two values based on
		/// the result of the condition. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string LogicalAndOperatorName = "op_LogicalAnd";

		/// <summary>
		/// The name of the logical OR operator, which is used to evaluate two boolean expressions and return true if either
		/// of the expressions is true. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string LogicalOrOperatorName = "op_LogicalOr";

		/// <summary>
		/// The name of the member selection operator, which is used to access a member of a type. In C#, this operator can be
		/// overloaded in user-defined types.
		/// </summary>
		public const string MemberSelectionOperatorName = "op_MemberSelection";

		/// <summary>
		/// The name of the pointer dereference operator, which is used to access the value pointed to by a pointer. In C#,
		/// this operator can be overloaded in user-defined types.
		/// </summary>
		public const string PointerDereferenceOperatorName = "op_PointerDereference";

		/// <summary>
		/// The name of the pointer to member selection operator, which is used to access a member of a type through a pointer.
		/// In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string PointerToMemberSelectionOperatorName = "op_PointerToMemberSelection";

		/// <summary>
		/// The name of the signed right shift operator, which is used to shift the bits of a value to the right by a specified
		/// number of positions. In C#, this operator can be overloaded in user-defined types.
		/// </summary>
		public const string SignedRightShiftOperatorName = "op_SignedRightShift";
	}
}
