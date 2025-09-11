using System;
using System.Collections.Generic;

namespace GRF.FileFormats.LubFormat.VM {
	public partial class OpCodes {
		public enum ConditionToken {
			Single,
			And,
			Or,
		}

		public class RelationalNode {
			public RelationalNode Left;
			public RelationalNode Right;
			public ConditionToken Token = ConditionToken.Single;

			public ConditionToken FinalToken {
				get {
					if (Token == ConditionToken.Single || !IsReversed)
						return Token;
					return Token == ConditionToken.And ? ConditionToken.Or : ConditionToken.And;
				}
			}

			protected string _value;
			public bool IsReversed { get; private set; }

			public RelationalNode() {
			}

			public RelationalNode(string value) {
				_value = value;
			}

			public void Reverse() {
				if (Left != null)
					Left.Reverse();
				if (Right != null)
					Right.Reverse();

				IsReversed = !IsReversed;
			}

			public override string ToString() {
				if (_value != null) {
					if (IsReversed) {
						string value = _value;
						value = RelationalStatement.InternalReverse(value);
						return value;
					}

					return _value;
				}

				var token = FinalToken;

				if (token == ConditionToken.Single)
					throw new Exception("Unexpected token");

				string rel_op = "";

				switch(token) {
					case ConditionToken.And:
						rel_op = " and ";
						break;
					case ConditionToken.Or:
						rel_op = " or ";
						break;
				}

				if ((Left.FinalToken == ConditionToken.Single || Left.FinalToken == token) && (Right.FinalToken == ConditionToken.Single || Right.FinalToken == token)) {
					return Left + rel_op + Right;
				}
				if (Right.FinalToken == ConditionToken.Single) {
					return "(" + Left + ")" + rel_op + Right;
				}
				if (Left.FinalToken == ConditionToken.Single) {
					return Left + rel_op + "(" + Right + ")";
				}
				if (Left.FinalToken != token && Right.FinalToken == token) {
					return "(" + Left + ")" + rel_op + Right;
				}
				if (Right.FinalToken != token && Left.FinalToken == token) {
					return Left + rel_op + "(" + Right + ")";
				}
				return "(" + Left + ")" + rel_op + "(" + Right + ")";
			}
		}

		public class RelationalStatement {
			private RelationalNode _conditionNode;

			public RelationalNode Node {
				get { return _conditionNode; }
			}

			public delegate void ConditionChanged(object sender);

			public event ConditionChanged Changed;

			protected virtual void OnChanged() {
				ConditionChanged handler = Changed;
				if (handler != null) handler(this);
			}

			public RelationalStatement(string condition) {
				_conditionNode = new RelationalNode(condition);
			}

			internal static string InternalSwapOperands(string condition) {
				List<string[]> conds = new List<string[]> {
					new string[] { " == ", " == " },
					new string[] { " ~= ", " ~= " },
					new string[] { " < ", " > " },
					new string[] { " > ", " < " },
					new string[] { " >= ", " <= " },
					new string[] { " <= ", " >= " },
				};

				foreach (var cond_i in conds) {
					if (condition.Contains(cond_i[0])) {
						condition = condition.Replace(cond_i[0], cond_i[1]);
						return condition;
					}
				}

				return condition;
			}

			internal static string InternalReverse(string condition) {
				bool replaced = false;

				List<string[]> conds = new List<string[]> {
					new string[] { " == ", " ~= " },
					new string[] { " < ", " >= " },
					new string[] { " > ", " <= " },
				};

				foreach (var cond_i in conds) {
					if (condition.Contains(cond_i[0])) {
						condition = condition.Replace(cond_i[0], cond_i[1]);
						replaced = true;
						break;
					}

					if (condition.Contains(cond_i[1])) {
						condition = condition.Replace(cond_i[1], cond_i[0]);
						replaced = true;
						break;
					}
				}

				if (replaced)
					return condition;

				if (condition.StartsWith("not(")) {
					condition = condition.Substring(4, condition.Length - 5);

					// ?? Change token to previous?
					return condition;
				}

				condition = "not(" + condition + ")";
				return condition;
			}

			public void Reverse() {
				_conditionNode.Reverse();
				OnChanged();
			}

			public void Combine(RelationalStatement right, ConditionToken token) {
				_conditionNode = new RelationalNode { Token = token, Left = _conditionNode, Right = right.Node };
				OnChanged();
			}

			public override string ToString() {
				return _conditionNode.ToString();
			}
		}
	}
}