using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PM02_421_Maslo
{
    public partial class MainWindow : Window
    {
        private int variablesCount;
        private int constraintsCount;
        private TextBox[,] coefficientsMatrix;
        private TextBox[] resultsTextBoxes;
        private TextBox[] objectiveFunctionTextBoxes;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(VariablesCountTextBox.Text, out variablesCount) || variablesCount <= 0 ||
                !int.TryParse(ConstraintsCountTextBox.Text, out constraintsCount) || constraintsCount <= 0)
            {
                MessageBox.Show("Введите корректное количество переменных и ограничений.");
                return;
            }

            InputPanel.Visibility = Visibility.Visible;

            // Очищаем предыдущие данные
            CoefficientsMatrixGrid.Children.Clear();
            CoefficientsMatrixGrid.RowDefinitions.Clear();
            CoefficientsMatrixGrid.ColumnDefinitions.Clear();
            ResultsPanel.Children.Clear();
            ObjectiveFunctionGrid.Children.Clear();

            // Целевая функция(запасы)
            ObjectiveFunctionGrid.Columns = variablesCount;
            objectiveFunctionTextBoxes = new TextBox[variablesCount];
            for (int j = 0; j < variablesCount; j++)
            {
                var box = new TextBox
                {
                    Width = 50,
                    Margin = new Thickness(3),
                    TextAlignment = TextAlignment.Center
                };
                ObjectiveFunctionGrid.Children.Add(box);
                objectiveFunctionTextBoxes[j] = box;
            }

            // Матрица коэффициентов
            for (int i = 0; i < constraintsCount; i++)
                CoefficientsMatrixGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            for (int j = 0; j < variablesCount; j++)
                CoefficientsMatrixGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            coefficientsMatrix = new TextBox[constraintsCount, variablesCount];
            for (int i = 0; i < constraintsCount; i++)
            {
                for (int j = 0; j < variablesCount; j++)
                {
                    var box = new TextBox
                    {
                        Width = 50,
                        Margin = new Thickness(3),
                        TextAlignment = TextAlignment.Center
                    };
                    Grid.SetRow(box, i);
                    Grid.SetColumn(box, j);
                    CoefficientsMatrixGrid.Children.Add(box);
                    coefficientsMatrix[i, j] = box;
                }
            }

            // Ввод свободных членов ограничений
            resultsTextBoxes = new TextBox[constraintsCount];
            for (int i = 0; i < constraintsCount; i++)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                var label = new TextBlock
                {
                    Text = $"b{i + 1} = ",
                    VerticalAlignment = VerticalAlignment.Center
                };
                var box = new TextBox
                {
                    Width = 60,
                    Margin = new Thickness(5, 0, 0, 0),
                    TextAlignment = TextAlignment.Center
                };
                panel.Children.Add(label);
                panel.Children.Add(box);
                ResultsPanel.Children.Add(panel);
                resultsTextBoxes[i] = box;
            }
        }

        private void SolveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double[,] matrix = new double[constraintsCount, variablesCount];
                double[] results = new double[constraintsCount];
                double[] objectiveFunction = new double[variablesCount];

                // Чтение коэффициентов ограничений
                for (int i = 0; i < constraintsCount; i++)
                    for (int j = 0; j < variablesCount; j++)
                        matrix[i, j] = double.Parse(coefficientsMatrix[i, j].Text);

                // Чтение свободных членов
                for (int i = 0; i < constraintsCount; i++)
                    results[i] = double.Parse(resultsTextBoxes[i].Text);

                // Чтение целевой функции(запасов)
                for (int j = 0; j < variablesCount; j++)
                    objectiveFunction[j] = double.Parse(objectiveFunctionTextBoxes[j].Text);

                // Решение симплекс-методом
                double maxValue;
                double[] solution = SimplexSolver.Solve(matrix, results, objectiveFunction, out maxValue);

                string result = "Решение задачи:\n";
                for (int i = 0; i < solution.Length; i++)
                    result += $"x{i + 1} = {solution[i]:F2}\n";

                result += $"Максимальная выручка: {maxValue:F2}";
                ResultTextBlock.Text = result;
            }
            catch (FormatException)
            {
                MessageBox.Show("Во всех ячейках должны быть числа!", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Нет решения", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        public class SimplexSolver
        {
            public static double[] Solve(double[,] A, double[] b, double[] c, out double maxValue)
            {
                int m = b.Length;
                int n = c.Length;
                int size = n + m;

                double[,] tableau = new double[m + 1, size + 1];

                // Заполняем таблицу коэффициентами ограничений
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                        tableau[i, j] = A[i, j];

                    tableau[i, n + i] = 1;
                    tableau[i, size] = b[i];
                }

                // Заполняем строку целевой функции (помним, что симплекс ищет min, поэтому c отрицательные)
                for (int j = 0; j < n; j++)
                    tableau[m, j] = -c[j];

                // Сам алгоритм
                while (true)
                {
                    // Находим ведущий столбец (максимальный по модулю отрицательный элемент в последней строке)
                    int pivotCol = -1;
                    double minValue = 0;
                    for (int j = 0; j < size; j++)
                    {
                        if (tableau[m, j] < minValue)
                        {
                            minValue = tableau[m, j];
                            pivotCol = j;
                        }
                    }

                    // Если нет отрицательных — нашли максимум
                    if (pivotCol == -1)
                        break;

                    // Находим ведущую строку (по минимальному положительному отношению)
                    int pivotRow = -1;
                    double minRatio = double.PositiveInfinity;
                    for (int i = 0; i < m; i++)
                    {
                        if (tableau[i, pivotCol] > 0)
                        {
                            double ratio = tableau[i, size] / tableau[i, pivotCol];
                            if (ratio < minRatio)
                            {
                                minRatio = ratio;
                                pivotRow = i;
                            }
                        }
                    }

                    if (pivotRow == -1)
                        throw new InvalidOperationException("Задача не имеет решения.");

                    // Опорный элемент
                    double pivotElement = tableau[pivotRow, pivotCol];

                    // Делим опорную строку
                    for (int j = 0; j <= size; j++)
                        tableau[pivotRow, j] /= pivotElement;

                    // Обнуляем остальные строки
                    for (int i = 0; i <= m; i++)
                    {
                        if (i == pivotRow) continue;
                        double factor = tableau[i, pivotCol];
                        for (int j = 0; j <= size; j++)
                            tableau[i, j] -= factor * tableau[pivotRow, j];
                    }
                }

                // Читаем решение
                double[] solution = new double[n];
                for (int j = 0; j < n; j++)
                {
                    int basicRow = -1;
                    for (int i = 0; i < m; i++)
                    {
                        if (Math.Abs(tableau[i, j] - 1) < 1e-6)
                        {
                            if (basicRow == -1)
                                basicRow = i;
                            else
                            {
                                basicRow = -1;
                                break;
                            }
                        }
                        else if (Math.Abs(tableau[i, j]) > 1e-6)
                        {
                            basicRow = -1;
                            break;
                        }
                    }
                    if (basicRow != -1)
                        solution[j] = tableau[basicRow, size];
                    else
                        solution[j] = 0;
                }

                maxValue = tableau[m, size];
                return solution;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            VariablesCountTextBox.Clear();
            ConstraintsCountTextBox.Clear();
            CoefficientsMatrixGrid.Children.Clear();
            CoefficientsMatrixGrid.RowDefinitions.Clear();
            CoefficientsMatrixGrid.ColumnDefinitions.Clear();
            ResultsPanel.Children.Clear();
            ObjectiveFunctionGrid.Children.Clear();
            ResultTextBlock.Text = "";
            InputPanel.Visibility = Visibility.Collapsed;
        }

        private void SaveToFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ResultTextBlock.Text))
            {
                MessageBox.Show("Нет данных для сохранения!", "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt",
                FileName = "РешениеСимплексМетода.txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, ResultTextBlock.Text);
                    MessageBox.Show("Результат успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
