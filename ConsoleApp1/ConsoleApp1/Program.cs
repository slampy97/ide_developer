using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;



namespace MyTests
{
    public interface IExpressionVisitor
    {
        void Visit(Literal expression);
        void Visit(Variable expression);
        void Visit(BinaryExpression expression);
        void Visit(ParenExpression expression);
    }
    
    public interface IExpression
    {
        void Accept(IExpressionVisitor visitor);
    }

    public class Literal : IExpression
    {
        public Literal(string value)
        {
            Value = value;
        }

        public readonly string Value;
        
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class Variable : IExpression
    {
        public Variable(string name)
        {
            Name = name;
        }

        public readonly string Name;
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    public class BinaryExpression : IExpression
    {
        public readonly IExpression FirstOperand;
        public readonly IExpression SecondOperand;
        public readonly string Operator;

        public BinaryExpression(IExpression firstOperand, IExpression secondOperand, string @operator)
        {
            FirstOperand = firstOperand;
            SecondOperand = secondOperand;
            Operator = @operator;
        }

        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    public class ParenExpression : IExpression
    {
        public ParenExpression(IExpression operand)
        {
            Operand = operand;
        }

        public readonly IExpression Operand;
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class DumpVisitor : IExpressionVisitor
    {
        private readonly StringBuilder myBuilder;

        public DumpVisitor()
        {
            myBuilder = new StringBuilder();
        }

        public void Visit(Literal expression)
        {
            myBuilder.Append("Literal(" + expression.Value + ")");
        }

        public void Visit(Variable expression)
        {
            myBuilder.Append("Variable(" + expression.Name + ")");
        }

        public void Visit(BinaryExpression expression)
        {
            myBuilder.Append("Binary(");
            expression.FirstOperand.Accept(this);
            myBuilder.Append(expression.Operator);
            expression.SecondOperand.Accept(this);
            myBuilder.Append(")");
        }

        public void Visit(ParenExpression expression)
        {
            myBuilder.Append("Paren(");
            expression.Operand.Accept(this);
            myBuilder.Append(")");
        }

        public override string ToString()
        {
            return myBuilder.ToString();
        }
    }
    
    
    public class SimpleParser
    {
        public static IExpression Parse(string text)
        {
            Stack<char> operands = new Stack<char>();
            StringBuilder res = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(')
                {
                    operands.Push('(');
                }
                else if (text[i] == ')')
                {
                    while (true)
                    {
                        var topElement = operands.Peek();
                        if (topElement == '(')
                        {
                            operands.Pop();
                            break;
                        }
                        else
                        {
                            res.Append(topElement);
                            operands.Pop();
                        }
                    }
                }
                else if (text[i] == '+' || text[i] == '-')
                {
                    while (operands.Count != 0)
                    {
                        var topElement = operands.Peek();
                        if (topElement == '+' || topElement == '-' || topElement == '*' || topElement == '/')
                        {
                            res.Append(topElement);
                            operands.Pop();
                        }
                        else
                        {
                            break;
                        }
                    }
                    operands.Push(text[i]);
                }
                else if (text[i] == '*' || text[i] == '/')
                {
                    while (operands.Count != 0)
                    {
                        var topElement = operands.Peek();
                        if ( topElement == '*' || topElement == '/')
                        {
                            res.Append(topElement);
                            operands.Pop();
                        }
                        else
                        {
                            break;
                        }
                    }
                    operands.Push(text[i]);
                }
                else if (char.IsDigit(text[i]))
                {
                    res.Append(text[i]);
                }
                else if (char.IsLetter(text[i]))
                {
                    res.Append(text[i]);
                }
            }

            while (operands.Count != 0)
            {
                res.Append(operands.Peek());
                operands.Pop();
            }

            var treeStack = new Stack<IExpression>();

            for (int i = 0; i < res.Length; i++)
            {
                if (res[i] == '+' || res[i] == '-' || res[i] == '*' || res[i] == '/')
                {
                    var fstOp = treeStack.Peek();
                    treeStack.Pop();
                    var sndOp = treeStack.Peek();
                    treeStack.Pop();
                    treeStack.Push( new BinaryExpression(sndOp, fstOp, res[i].ToString()));
                }
                else if (char.IsDigit(res[i]))
                {
                    treeStack.Push(new Literal(res[i].ToString()));
                }
                else if (char.IsLetter(res[i]))
                {
                    treeStack.Push(new Variable(res[i].ToString()));
                }
            }
            return treeStack.Peek();
        }
    }
    
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("1+2").Accept(dumpVisitor);
            Assert.AreEqual("Binary(Literal(1)+Literal(2))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test2()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("1+x").Accept(dumpVisitor);
            Assert.AreEqual("Binary(Literal(1)+Variable(x))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test3()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("(1+x)*(2/4)").Accept(dumpVisitor);
            Assert.AreEqual("Binary(Binary(Literal(1)+Variable(x))*Binary(Literal(2)/Literal(4)))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test4()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("(d*e+f)*g").Accept(dumpVisitor);
            Assert.AreEqual("Binary(Binary(Binary(Variable(d)*Variable(e))+Variable(f))*Variable(g))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test5()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("a+b*c").Accept(dumpVisitor);
            Assert.AreEqual("Binary(Variable(a)+Binary(Variable(b)*Variable(c)))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test6()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("a+b*c+(d*e+f)*g").Accept(dumpVisitor);
            const string fstPart = "Binary(Variable(a)+Binary(Variable(b)*Variable(c)))";
            const string sndPart = "Binary(Binary(Binary(Variable(d)*Variable(e))+Variable(f))*Variable(g))";
            Assert.AreEqual("Binary(" + fstPart + "+" + sndPart + ")", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test7()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("(1+x)/(2+(4/y))*(2-x)").Accept(dumpVisitor);
            const string fstPart = "Binary(Literal(1)+Variable(x))";
            const string sndPart = "Binary(Literal(2)+Binary(Literal(4)/Variable(y)))";
            const string thirdPart = "Binary(Literal(2)-Variable(x))";
            const string composeFstSnd = "Binary(" + fstPart + "/" + sndPart + ")";
            const string final = "Binary(" + composeFstSnd + "*" + thirdPart + ")";
            Assert.AreEqual(final, dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test8()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("(1-2+x)*y+5*(z/8)").Accept(dumpVisitor);
            Assert.AreEqual("Binary(Binary(Binary(Binary(Literal(1)-Literal(2))+Variable(x))*Variable(y))+Binary(Literal(5)*Binary(Variable(z)/Literal(8))))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test9()
        {
            var dumpVisitor = new DumpVisitor();
            SimpleParser.Parse("1+(1+2*x)/(8/z+2*(3+9))").Accept(dumpVisitor);
            const string fst = "Binary(Literal(1)+Binary(Literal(2)*Variable(x)))";
            const string snd = "Binary(Binary(Literal(8)/Variable(z))+Binary(Literal(2)*Binary(Literal(3)+Literal(9))))";
            const string comboFstSnd = "Binary(" + fst + "/" + snd + ")";
            const string final = "Binary(Literal(1)+" + comboFstSnd + ")";
            Assert.AreEqual(final, dumpVisitor.ToString());
            Assert.Pass();
        }
        
    }
}