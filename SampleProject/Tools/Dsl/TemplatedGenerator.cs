﻿using System;
using System.CodeDom.Compiler;
using System.Linq;

namespace Dsl
{
	public sealed class TemplatedGenerator : IGenerateCode
	{
        public string ClassNameTemplate { get; set; }
        public string MemberTemplate { get; set; }
        public string PrivateCtorTemplate { get; set; }
        public string Namespace { get; set; }
        public string Region { get; set; }
        public string GenerateInterfaceForEntityWithModifiers { get; set; }
        public string TemplateForInterfaceName { get; set; }
        public string TemplateForInterfaceMember { get; set; }
		public TemplatedGenerator()
		{
		    Region = "Generated by Lokad Code DSL";
		    ClassNameTemplate = @"
[ProtoContract]
public sealed class {0}";

		    MemberTemplate = @"[ProtoMember({0})] public readonly {1} {2};";
		    PrivateCtorTemplate = @"
private {0} () {{}}";
		    TemplateForInterfaceName = "public interface I{0}";
		    TemplateForInterfaceMember = "void When({0} c)";
		    GenerateInterfaceForEntityWithModifiers = "none";
		    //
		}

        public void Generate(Context context, IndentedTextWriter outer)
		{
			var writer = new CodeWriter(outer);
            if (!string.IsNullOrEmpty(Namespace))
            {
                writer.WriteLine("namespace {0}", Namespace);
                writer.WriteLine("{");
            }
            writer.Indent += 1;

            if (!string.IsNullOrEmpty(Region))
            {
                writer.WriteLine("#region {0}", Region);
            }

			WriteContext(writer, context);


            if (!string.IsNullOrEmpty(Region))
            {
                writer.WriteLine("#endregion");
            }

            writer.Indent -= 1;

            if (!string.IsNullOrEmpty(Namespace))
            {
                writer.WriteLine("}");
            }
		}

        private void WriteContext(CodeWriter writer, Context context)
	    {
	        foreach (var contract in context.Contracts)
	        {
	            writer.Write(ClassNameTemplate, contract.Name);
	            
	            if (contract.Modifiers.Any())
	            {
                    writer.Write(" : {0}", string.Join(", ", contract.Modifiers.Select(s => s.Interface).ToArray()));
	            }
	            writer.WriteLine();

	            writer.WriteLine("{");
	            writer.Indent += 1;

	            if (contract.Members.Count > 0)
	            {
	                WriteMembers(contract, writer);
	                writer.WriteLine(PrivateCtorTemplate, contract.Name);
	                writer.Write("public {0} (", contract.Name);
	                WriteParameters(contract, writer);
	                writer.WriteLine(")");
	                writer.WriteLine("{");

	                writer.Indent += 1;
	                WriteAssignments(contract, writer);
	                writer.Indent -= 1;

	                writer.WriteLine("}");
					
	            }


	            writer.Indent -= 1;
	            writer.WriteLine("}");
	        }
            foreach (var entity in context.Entities)
            {
                if ((entity.Name ?? "null") == "null")
                    continue;

                GenerateEntityInterface(entity, writer, "?", "public interface I{0}Aggregate");
                GenerateEntityInterface(entity, writer, "!", "public interface I{0}AggregateState");
            }
	    }
        void GenerateEntityInterface(Entity entity, CodeWriter writer, string member, string template)
        {
            var ms = member.Split(',');
            var matches = entity.Messages.Where(m => m.Modifiers.Select(s => s.Identifier).Intersect(ms).Any()).ToList();
            if (matches.Any())
            {
                writer.WriteLine();
                writer.WriteLine(template, entity.Name);
                writer.WriteLine("{");
                writer.Indent += 1;
                foreach (var contract in matches)
                {
                    writer.WriteLine("void When({0} c);", contract.Name);
                }
                writer.Indent -= 1;
                writer.WriteLine("}");
            }
        }



	    void WriteMembers(Message message, CodeWriter writer)
		{
			var idx = 1;
			foreach (var member in message.Members)
			{
                writer.WriteLine(MemberTemplate, idx, member.Type, GeneratorUtil.MemberCase(member.Name));

				
				idx += 1;
			}
		}
        void WriteParameters(Message message, CodeWriter writer)
		{
			var first = true;
			foreach (var member in message.Members)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					writer.Write(", ");
				}
				writer.Write("{0} {1}", member.Type, GeneratorUtil.ParameterCase(member.Name));
			}
		}

        void WriteAssignments(Message message, CodeWriter writer)
		{
			foreach (var member in message.Members)
			{
				writer.WriteLine("{0} = {1};", GeneratorUtil.MemberCase(member.Name), GeneratorUtil.ParameterCase(member.Name));
			}
		}
	}

    public sealed class CodeWriter
    {
        private readonly IndentedTextWriter _writer;

        public CodeWriter(IndentedTextWriter writer)
        {
            _writer = writer;
        }

        public int Indent { get { return _writer.Indent; } set { _writer.Indent = value; } }
        public void Write(string format, params object[] args)
        {
            var txt = string.Format(format, args);
            var lines = txt.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            

            for (int i = 0; i < lines.Length; i++)
            {
                bool thisIsLast = i == (lines.Length - 1);
                if (thisIsLast)
                    _writer.Write(lines[i]);
                else
                    _writer.WriteLine(lines[i]);

            }
        }

        public void WriteLine()
        {
            _writer.WriteLine();
        }

        public void WriteLine(string format, params object[] args)
        {
            
            var txt = args.Length == 0 ? format : string.Format(format, args);
            var lines = txt.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            

            foreach (string t in lines)
            {
                _writer.WriteLine(t);
            }
        }
    }
}