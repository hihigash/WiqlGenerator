using Azure;
using Azure.AI.OpenAI;
using System.Text.RegularExpressions;

if (args.Length != 1)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Please input text to generate query.");
    Console.ResetColor();
    return;
}

try
{
    var text = args[0];
    OpenAIClient client = new OpenAIClient(
        new Uri(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_ENDPOINT")),
        new AzureKeyCredential(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")));

    Response<ChatCompletions> response = await client.GetChatCompletionsAsync(
        "gpt-3",
        new ChatCompletionsOptions()
        {
            Messages =
            {
            new ChatMessage(ChatRole.System, @"As intelligent query generator, You generate query for Azure DevOps / Azure Boards using work item query language (WIQL). If you need to more information about WIQL, please read following links.
 - https://learn.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=azure-devops
 - https://learn.microsoft.com/en-us/azure/devops/boards/queries/query-operators-variables?view=azure-devops
 - https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops

The specifications written in EBNF are:
""""""
(* FLATSELECT productions*)
FlatSelect = Select FieldList 
From WorkItems
[Where LogicalExpression]
[OrderBy OrderByFieldList]
[Asof DateTime];
FieldList = Field [Comma FieldList];
Field = Identifier | LSqBracket Identifier RSqBracket;
LogicalExpression = [Not| Ever] ConditionalExpression [(And | Or) LogicalExpression];
ConditionalExpression = LParen LogicalExpression RParen 
| Field ConditionalOperator Value
| Field IsEmpty
| Field IsNotEmpty
| Field [Not] In LParen ValueList RParen;
Number = [Minus] Digits;
VariableArguments = (String | Number | True | False) [Comma VariableArguments];
VariableExpression = Variable [LParen VariableArguments RParen] [(Plus | Minus) Number];
Value = Number | String | DateTime | Field | VariableExpression | True | False;
DateTime = String;
ValueList = Value [Comma ValueList];
ConditionalOperator = Equals | NotEquals | LessThan | LessOrEq | GreaterThan 
| GreaterOrEq | [Ever] [Not] (Like | Under) | [Not] Contains | [Not] ContainsWords
| [Not] InGroup | Ever;
ContainsWords = Contains Words;
InGroup = In Group;
OrderByFieldList = Field [Asc | Desc] [Comma OrderByFieldList];

(* one hop query *)
OneHopSelect = Select FieldList
From WorkItemLinks
[Where LinkExpression]
[OrderBy LinkOrderByFieldList]
[Asof DateTime]
[Mode LParen (MustContain | MayContain | DoesNotContain) RParen];
LinkOrderByFieldList = [SourcePrefix | TargetPrefix]
    Field [Asc | Desc] 
    [Comma LinkOrderByFieldList];
LinkExpression = [Not | Ever] LinkCondition [(And | Or) LinkExpression];
SourcePrefix = Source Dot | LSqBracket Source RSqBracket Dot;
TargetPrefix = Target Dot | LSqBracket Target RSqBracket Dot;
LinkCondition = LParen LinkExpression RParen 
| [SourcePrefix | TargetPrefix] Field ConditionalOperator Value
| [SourcePrefix | TargetPrefix] Field IsEmpty
| [SourcePrefix | TargetPrefix] Field IsNotEmpty
| (SourcePrefix | TargetPrefix) Field [Not] In LParen ValueList RParen;

RecursiveSelect = Select FieldList
From WorkItemLinks
(* Don't check single link type at parse time *)
[Where LinkExpression]
[OrderBy LinkOrderByFieldList]
[Asof DateTime]
[Mode LParen 
    (
        Recursive |
        MatchingChildren |
        Recursive Comma ReturnMatchingChildren |
        MatchingChildren Comma Recursive  
    )
RParen];
"""""""),
            new ChatMessage(ChatRole.User, text),
            },
            Temperature = (float)0.5,
            MaxTokens = 800,
            NucleusSamplingFactor = (float)0.95,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        });

    ChatCompletions completions = response.Value;
    var resultText = completions.Choices.First().Message.Content;
    var match = Regex.Match(resultText, @"```(.*?)```", RegexOptions.Singleline);
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine(match.Groups[1].Value.Trim('\n'));
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(ex.Message);
    Console.ResetColor();
}
