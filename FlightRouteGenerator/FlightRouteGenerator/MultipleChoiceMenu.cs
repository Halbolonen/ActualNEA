using System.Linq.Expressions;

namespace FlightRouteGenerator
{
    internal static class MultipleChoiceMenu
    {
        private static string arrow = "-->";
        private static string blankSpace = "   ";
        private static ConsoleColor accentColor = ConsoleColor.Green;

        private static void ClearInputBuffer()
        {
            while (Console.KeyAvailable)
            {
                Console.ReadKey();
            }
        }

        private static void DrawOptions(List<string> OptionList, HashSet<int> selectedOptions, ConsoleColor baseColor, int baseCursorLeft, int baseCursorTop)
        {
            Console.CursorLeft = baseCursorLeft;
            Console.CursorTop = baseCursorTop;
            for (int i = 0; i < OptionList.Count; i++)
            {
                if (selectedOptions.Contains(i))
                {
                    Console.ForegroundColor = accentColor;
                }
                Console.WriteLine($"{i + 1}) {OptionList[i]}");

                Console.ForegroundColor = baseColor;
            }
        }

        public static HashSet<int> GetMultiSelectChoice(List<string> optionList)
        {
            int baseCursorLeft = Console.CursorLeft;
            int baseCursorTop = Console.CursorTop;
            HashSet<int> selectedOptions = new HashSet<int>();
            bool choiceMade = false;
            ConsoleColor baseColor = Console.ForegroundColor;

            DrawOptions(optionList, selectedOptions, baseColor, baseCursorLeft, baseCursorTop);

            ClearInputBuffer();
            Console.WriteLine("\nSelect one OR MORE options from the list above.\nUse the numbers on your keyboard to toggle selection for an option.\nPress enter to confirm your selection.\nDo not use the numpad.\n");
            int belowTextCursorTop = Console.CursorTop;
            Console.CursorVisible = false;
            while (!choiceMade)
            {
                ConsoleKey input = Console.ReadKey(true).Key;
                ClearInputBuffer();

                if (input == ConsoleKey.Enter && selectedOptions.Count > 0)
                {
                    choiceMade = true;
                }
                else if (input != ConsoleKey.Enter)
                {
                    int selectedOption = Convert.ToInt32(input - ConsoleKey.D0 - 1);

                    if (selectedOption > -1 && selectedOption < optionList.Count)
                    {
                        if (!selectedOptions.Contains(selectedOption))
                        {
                            selectedOptions.Add(selectedOption);
                        }
                        else
                        {
                            selectedOptions.Remove(selectedOption);
                        }
                    }
                }

                DrawOptions(optionList, selectedOptions, baseColor, baseCursorLeft, baseCursorTop);
                Console.WriteLine("\n\n");
            }

            Console.CursorTop = belowTextCursorTop;
            Console.CursorLeft = baseCursorLeft;
            return selectedOptions;
        }

        public static int GetSingleSelectChoice(List<string> optionList)
        {
            int baseCursorLeft = Console.CursorLeft;
            int baseCursorTop = Console.CursorTop;
            int selectedOption = -1;
            bool choiceMade = false;
            ConsoleColor baseColor = Console.ForegroundColor;

            DrawOptions(optionList, new HashSet<int> { selectedOption }, baseColor, baseCursorLeft, baseCursorTop);

            ClearInputBuffer();

            Console.WriteLine("\nSelect an option from the list above.\nUse the numbers on your keyboard to highlight an option, and press enter to select it.\nDo not use the numpad.");
            int belowTextCursorTop = Console.CursorTop;
            Console.CursorVisible = false;
            while (!choiceMade)
            {
                ConsoleKey input = Console.ReadKey(true).Key;
                ClearInputBuffer();

                if (input == ConsoleKey.Enter && selectedOption != -1)
                {
                    choiceMade = true;
                }
                else if (input != ConsoleKey.Enter)
                {
                    int highlightedOption = Convert.ToInt32(input - ConsoleKey.D0 - 1);

                    if (highlightedOption < 0 || highlightedOption >= optionList.Count || highlightedOption == selectedOption)
                    {
                        selectedOption = -1;
                    }
                    else
                    {
                        selectedOption = highlightedOption;
                    }
                }

                DrawOptions(optionList, new HashSet<int> { selectedOption }, baseColor, baseCursorLeft, baseCursorTop);
                Console.WriteLine("\n\n");

            }

            Console.CursorTop = belowTextCursorTop;
            Console.CursorLeft = baseCursorLeft;
            return selectedOption;
        }
    }
}
