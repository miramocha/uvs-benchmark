using System;
using System.CodeDom;
using System.Collections.Generic;

namespace Unity.VisualScripting
{
    [AotStubWriter(typeof(Type))]
    public class StructDefaultConstructorStubWriter : AotStubWriter
    {
        public StructDefaultConstructorStubWriter(Type type) : base(type) { }
        
        private new Type stub => (Type)base.stub; 
        public override bool skip => !stub.IsStruct();

        public override string stubMethodComment => stub.CSharpFullName();

        public override string stubMethodName => stubMethodComment.FilterReplace('_', true, symbols: false, whitespace: false, punctuation: false) + "_Struct_Default_Constructor";
        public override IEnumerable<CodeStatement> GetStubStatements()
        {
            var typeRef = new CodeTypeReference(stub);

            var variableDeclaration = new CodeVariableDeclarationStatement(
                typeRef,
                "stubInstance",
                new CodeDefaultValueExpression(typeRef)
            );

            yield return variableDeclaration;

            var invokerGenericType = typeof(OptimizedStructDefaultConstructorInvoker<>);
            var invokerTypeRef = new CodeTypeReference(invokerGenericType);
            invokerTypeRef.TypeArguments.Add(new CodeTypeReference(stub));

            var invokerDeclaration = new CodeVariableDeclarationStatement(
                invokerTypeRef,
                "invoker",
                new CodeObjectCreateExpression(invokerTypeRef)
            );

            var invokeCall = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression("invoker"),
                nameof(OptimizedStructDefaultConstructorInvoker<int>.Invoke)
            );

            var invokeValueCall = new CodeMethodInvokeExpression(
                new CodeVariableReferenceExpression("invoker"),
                nameof(OptimizedStructDefaultConstructorInvoker<int>.InvokeValue)
            );

            yield return invokerDeclaration;
            yield return new CodeExpressionStatement(invokeValueCall);
            yield return new CodeExpressionStatement(invokeCall);
        }
    }
}
