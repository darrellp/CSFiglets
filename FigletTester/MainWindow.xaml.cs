using System;
using System.Windows;
using CSFiglet;

namespace FigletTester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			var names = FigletFont.Names();
			foreach (var name in names)
			{
				var font = FigletFont.FigletFromName(name);
				var arranger = new Arranger(font, 100, Justify.Center) {Text = name};
				Console.WriteLine(arranger.StringContents);
			}
		}
	}
}
