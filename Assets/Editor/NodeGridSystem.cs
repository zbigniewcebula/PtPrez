using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class NodeGrid : EditorWindow {
	public static GUIStyle	rightLabel	= new GUIStyle();

	public class Node {
		public class Connector {
			public class ClickEvent : UnityEvent<Connector> {}

			public enum Type {
				None,
				Input,
				Output
			}

			public string		text		= string.Empty;
			public Type			type		= Type.None;

			public ClickEvent	onClick		= new ClickEvent();

			public Node			Parent {
				get { return parent; }
			}
			protected Node		parent		= null;

			public Vector2		Position {
				get {
					return position;
				}
			}
			protected Vector2	position;

			public bool 		bound;
			public bool 		disabled;

			public int			index	= 0;

			public Connector(Node parentNode) {
				parent	= parentNode;
			}
			public Connector(Connector org) {
				parent		= org.parent;

				text		= org.text;
				type		= org.type;
			}

			public void render(Vector2 wndSize) {
				Color	last	= GUI.backgroundColor;
				if (bound) {
					GUI.backgroundColor	= Color.black;
				}
				EditorGUI.BeginDisabledGroup(disabled);
					EditorGUILayout.BeginHorizontal(GUILayout.Height(25f));
				if (type == Type.Input) {
					if (GUILayout.Button("", GUILayout.Width(10), GUILayout.Height(10))) {
						onClick.Invoke(this);
					}
					position	= parent.pos + new Vector2(5f, 27f * (index + 1));
					EditorGUILayout.LabelField(text, GUILayout.Width(wndSize.x / 2 - 20f));
				} else if (type == Type.Output) {
					EditorGUILayout.LabelField(text, NodeGrid.rightLabel, GUILayout.Width(wndSize.x / 2 - 23f));
					if (GUILayout.Button("", GUILayout.Width(10), GUILayout.Height(10))) {
						onClick.Invoke(this);
					}
					position	= parent.pos + new Vector2(wndSize.x - 5f, 27f * (index + 1));
				}
					EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
				GUI.backgroundColor	= last;
			}
		} 
		public class Connection {
			public Color	color	= Color.black;
			public int		width	= 5;

			public Connector	a	= null;
			public Connector	b	= null;

			public Connection(Connector A, Connector B) {
				a		= A;
				b		= B;
				a.bound	= true;
				b.bound	= true;	
			}

			public void render(Vector2 off) {
				if (a != null && b != null) {
					Vector2 start	= -off + a.Position;
					Vector2 end		= -off + b.Position;
					if (start.x > end.x) {
						Vector3	temp	= end;
						end				= start;
						start			= temp;
					}

					Vector3 startPos	= new Vector3(start.x, start.y, 0);
					Vector3 endPos		= new Vector3(end.x, end.y, 0);
					Vector3 startTan 	= startPos	+ Vector3.right	* 50;
					Vector3 endTan		= endPos	+ Vector3.left	* 50;

					Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.yellow, null, width);
				}
			}

			public bool contains(Connector x) {
				return a == x || b == x;
			}
			public bool contains(Node x) {
				return a.Parent == x || b.Parent == x;
			}
		} 

		//Meta
		private static int	ALL	= 0;
		protected int		ID;

		//Settings
		public string		title;
		public Vector2		pos;
		public Vector2		size;

		public int			snapTo	= 1;
		
		public UnityEvent	onRender		= new UnityEvent();

		protected List<Connector>	inps	= new List<Connector>();
		protected List<Connector>	outs	= new List<Connector>();

		private NodeGrid	parent;

		public Node(NodeGrid parentGrid) {
			ID	= ALL;
			++ALL;

			parent	= parentGrid;
		}
		public Node(Node org) {
			ID	= ALL;
			++ALL;

			parent	= org.parent;
			org.inps.ForEach(i => {
				Connector	n	= new Connector(i);
				n.index			= inps.Count;
				n.onClick.AddListener(me => {connectionManagement(me);});
				inps.Add(n);
			});
			org.outs.ForEach(i => {
				Connector	n	= new Connector(i);
				n.index			= outs.Count;
				n.onClick.AddListener(me => {connectionManagement(me);});
				outs.Add(n);
			});

			snapTo	= org.snapTo;
			pos		= org.pos;
			size	= org.size;

			title	= org.title;
		}

		public void render(Vector2 off) {
			Rect	newPos	= GUI.Window(ID, new Rect(pos.x - off.x, pos.y - off.y, size.x, size.y), drawNode, title);
			pos.x			= Mathf.Floor((newPos.x + off.x) / snapTo) * snapTo;
			pos.y			= Mathf.Floor((newPos.y + off.y) / snapTo) * snapTo;
		}

		protected void drawNode(int id) {
			EditorGUILayout.BeginHorizontal();	//Connections
				EditorGUILayout.BeginVertical();
					if (inps.Count > 0) {
						inps.ForEach(c => {
							c.render(size);
						});
					} else {
						EditorGUILayout.LabelField("", GUILayout.Width(size.x / 2 - 5f));
					}
				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical();
					outs.ForEach(c => {
						c.render(size);
					});
				EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical();	//Under
				onRender.Invoke();
			EditorGUILayout.EndVertical();

			GUI.DragWindow();
		}

		public Connector addConnector(string text, Connector.Type type) {
			Connector	newConn	= new Connector(this);
			newConn.type		= type;
			newConn.text		= text;
			if (type == Connector.Type.Input) {
				newConn.index	= inps.Count;
				inps.Add(newConn);
			} else if (type == Connector.Type.Output) {
				newConn.index	= outs.Count;
				outs.Add(newConn);
			}
			newConn.onClick.AddListener(me => {connectionManagement(me);});
			return newConn;
		}

		void connectionManagement(Node.Connector con) {
			var	existing = parent.conns.Find(co => co.contains(con));
			if (existing != null) {
				parent.connectFromObj		= existing.a == con? existing.b: existing.a;
				existing.a.bound	= false;
				existing.b.bound	= false;
				parent.conns.Remove(existing);
			} else {
				if (parent.connectFromObj == null) {
					parent.connectFromObj	= con;
				} else if (con.Parent != parent.connectFromObj.Parent) {
					if (con.type != parent.connectFromObj.type) {
						parent.conns.Add(new Node.Connection(parent.connectFromObj, con));
						parent.connectFromObj	= null;	
						parent.RemoveNotification();
					} else {
						parent.ShowNotification(new GUIContent("Cannot connect to same type of pin!"));
					}
				} else {
					parent.ShowNotification(new GUIContent("Cannot connect to same node!"));
				}
			}
		}
	}

	[MenuItem("CustomTools/NodeGridSystem")]
	public static void ShowWindow() {
		GetWindow(typeof(NodeGrid), false, "NodeGrid").minSize = new Vector2(
			500, 300
		);
	}

	Vector2	offset		= Vector2.zero;

	public List<Node>				nodes	= new List<Node>();
	public List<Node.Connection>	conns	= new List<Node.Connection>();

	public Node.Connector			connectFromObj	= null;

	void OnEnable() {
		rightLabel.alignment	= TextAnchor.UpperRight;

		//#1
		Node node	= new Node(this);
		node.title	= "Sender";
		node.size	= Vector2.one * 200f;
		node.snapTo	= 10;

		node.addConnector("A", Node.Connector.Type.Output);
		node.addConnector("B", Node.Connector.Type.Output);
		node.addConnector("C", Node.Connector.Type.Output);
		node.addConnector("A", Node.Connector.Type.Output);
		node.addConnector("B", Node.Connector.Type.Output);
		node.addConnector("C", Node.Connector.Type.Output);

		node.onRender.AddListener(() => {
			EditorGUILayout.LabelField("Test");
		});

		nodes.Add(node);

		//#2
		node		= new Node(this);
		node.title	= "Reciever";
		node.size	= Vector2.one * 200f;
		node.pos	= Vector2.right * 250f;
		node.snapTo	= 10;

		node.addConnector("a", Node.Connector.Type.Input);
		node.addConnector("b", Node.Connector.Type.Input);
		node.addConnector("c", Node.Connector.Type.Input);
		node.addConnector("a", Node.Connector.Type.Input);
		node.addConnector("b", Node.Connector.Type.Input);
		node.addConnector("c", Node.Connector.Type.Input);

		node.onRender.AddListener(() => {
			EditorGUILayout.LabelField("Test");
		});

		nodes.Add(node);
	}

	void OnGUI() {
		Handles.BeginGUI();
		Color last		= Handles.color;
			drawBackground(new Color(0.1f, 0.1f, 0.1f, 1f));
		Handles.color	= last;

		BeginWindows();
			nodes.ForEach(n => {
				n.render(offset);
			});
		EndWindows();
		conns.ForEach(c => {
			c.render(offset);
		});
		Handles.EndGUI();

		Event	e = Event.current;
		if (e.type == EventType.MouseDrag) {
			offset	-= e.delta;
			Repaint();
		}

		if (connectFromObj != null) {
			drawCurve(connectFromObj.Position - offset, e.mousePosition);
			Repaint();
		}
		if (e.button == 1) {
			connectFromObj	= null;
		}
	}

	void drawBackground(Color color) {
		EditorGUI.DrawRect(
			new Rect(Vector2.zero, position.size),
			color
		);
	}

	void drawCurve(Vector2 start, Vector2 end) {
		if (start.x > end.x) {
			Vector3	temp	= end;
			end				= start;
			start			= temp;
		}

		Vector3 startPos	= new Vector3(start.x, start.y, 0);
		Vector3 endPos		= new Vector3(end.x, end.y, 0);
		Vector3 startTan 	= startPos	+ Vector3.right	* 50;
		Vector3 endTan		= endPos	+ Vector3.left	* 50;

		Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.yellow, null, 1);
	}

	public List<Node>	getConnections(Node node) {
		return conns.FindAll(c => c.contains(node)).Select(c => c.a.Parent == node? c.b.Parent: c.a.Parent).ToList();
	}
	public List<Node>	getOutputNodes(Node node) {
		return conns.FindAll(c => c.contains(node) && c.b.Parent != node)
			.Select(c => c.b.Parent)
			.ToList();
	}
	public List<Node>	getInputNodes(Node node) {
		return conns.FindAll(c => c.contains(node) && c.a.Parent != node)
			.Select(c => c.a.Parent)
			.ToList();
	}
}
