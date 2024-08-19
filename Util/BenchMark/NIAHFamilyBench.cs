﻿using SharperLLM.API;
using SharperLLM.Util.Ext;

namespace SharperLLM.Util.BenchMark;
// ExampleOutput:
//         3PPF 6PPF 9PPF 12PPF 15PPF
// 1family
// 2family
// 3family
// 4family
// 5family
public class NIAHFamilyBench(iLLMAPI api, PromptBuilder pb, int maxFamilyCount, int ppfIncTimes, int ppfInterval, int rep, bool fixedSampleTime = true)
{
    class FamilyNode
    {
        public string person;
        public List<FamilyNode> childs = new();
        public FamilyNode parent;

        public FamilyNode GetAncestor()
        {
            if (parent == null) return this;
            return parent.GetAncestor();
        }

        public List<FamilyNode> GetAllChild()
        {
            List<FamilyNode> fn = new();
            if (childs != null)
            {
                foreach (var item in childs)
                {
                    fn.Add(item);
                    fn.AddRange(item.GetAllChild());
                }
            }
            return fn;
        }
        // This method will generate the shuffled string of family relationships.
        public static async Task<string> GenerateShuffledRelationships(List<FamilyNode> roots)
        {
            var relationships = new List<string>();
            foreach (var root in roots)
            {
                // Helper function to collect relationships.
                void CollectRelationships(FamilyNode node)
                {
                    if (node.parent != null)
                    {
                        relationships.Add(new Random().Next(5) switch
                        {
                            0 => $"{node.person}是{node.parent.person}的孩子",
                            1 => $"{node.person}是{node.parent.person}的女儿",
                            2 => $"{node.person}是{node.parent.person}的儿子",
                            3 => $"{node.parent.person}是{node.person}的父亲",
                            4 => $"{node.parent.person}是{node.person}的母亲"
                        });

                    }

                    foreach (var child in node.childs)
                    {
                        CollectRelationships(child);
                    }
                }

                // Start collecting relationships from the root.
                CollectRelationships(root);
            }
            // Shuffle the list of relationships.
            relationships.Shuffle();

            // Join the shuffled relationships into a single string.
            return string.Join(", ", relationships);
        }
    }

    List<FamilyNode> BuildFamilies(int familyCount, int ppf)
    {
        var random = new Random();
        var families = new List<FamilyNode>();
        for (int i = 0; i < familyCount; i++)
        {
            var root = new FamilyNode() { person = GenerateRandomName() };
            for (int j = 0; j < ppf - 1; j++)
            {
                var persons = root.GetAllChild();
                persons.Add(root);

                int idx = random.Next(persons.Count);

                var child = new FamilyNode
                {
                    parent = persons[idx],
                    person = " "
                };

                bool SameName = true;
                while (SameName)
                {
                    SameName = false;
                    child.person = GenerateRandomName();
                    persons.ForEach(x => SameName &= (x.person == child.person));
                }

                persons[idx].childs.Add(child);
            }
            families.Add(root);
        }
        return families;
    }

    private string GenerateRandomName()
    {
        // 生成随机的名字
        return $"{Guid.NewGuid().ToString("N").Substring(0, 5)}";
    }

    public float[,] RunBenchmark()
    {
        float[,] results = new float[maxFamilyCount, ppfIncTimes];

        for (int i = 1; i <= maxFamilyCount; i++)
        {
            for (int j = 1; j <= ppfIncTimes; j++)
            {


                if (fixedSampleTime)
                {
                    // 随机抽取 person

                    for (int k = 0; k < rep; k++)
                    {
                        var ppf = j * ppfInterval;
                        // 组织随机家庭树
                        var families = BuildFamilies(i, j * ppfInterval);
                        string desc = FamilyNode.GenerateShuffledRelationships(families).Result;
                        var allPersons = families.SelectMany(family => family.GetAllChild().Select(child => child.person)).ToList();
                        Console.WriteLine($"Now Testing: familyCount:{i}, ppf:{ppf}, rep:{k}");

                        var target = allPersons[new Random().Next(allPersons.Count)];
                        var family = families.First(f => f.GetAllChild().Any(child => child.person == target));
                        var anc = family.person;

                        if (AskQuestion(desc, target, anc, out float accuracy))
                        {
                            results[i - 1, j - 1] += accuracy / rep;
                        }
                    }
                }
                else//遍历所有非祖先的家庭成员
                {
                    for (int k = 0; k < rep; k++)
                    {
                        var ppf = j * ppfInterval;

                        // 组织随机家庭树
                        var families = BuildFamilies(i, j * ppfInterval);
                        string desc = FamilyNode.GenerateShuffledRelationships(families).Result;

                        Console.WriteLine($"Now Testing: familyCount:{i}, ppf:{ppf}, rep:{k}");
                        foreach (var family in families)
                        {
                            var anc = family.person;
                            foreach (var target in family.GetAllChild())
                            {
                                if (AskQuestion(desc, target.person, anc, out float accuracy))
                                {
                                    results[i - 1, j - 1] += accuracy / ((float)rep * families.Count * family.GetAllChild().Count);
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("结果是:" + results[i - 1, j - 1]);
            }
        }
        PrintFloatArray(results);
        return results;

        void PrintFloatArray(float[,] floats)
        {
            int rows = floats.GetLength(0);
            int cols = floats.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write(floats[i, j] + (j < cols - 1 ? ", " : ""));
                }
                Console.WriteLine(); // Move to the next line after finishing a row.
            }
        }
    }

    private bool AskQuestion(string desc, string target, string anc, out float accuracy)
    {
        pb.System = "你是一个AI助手，下面请仔细阅读以下文本，并回答问题";
        pb.Messages = [
            (desc, PromptBuilder.From.system),
        ($"上面的文本中描述了一个或多个家族的关系，请从上面的文本中找到{target}的最远古的祖先。注意请直接给出答案。", PromptBuilder.From.user),
        ];

        var prompt = pb.GetResult();
        prompt += $"\n好的,{target}最远古的祖先是：";
        string response = string.Empty;
        foreach (var token in api.GenerateTextAsync(prompt).ToBlockingEnumerable())
        {
            response += token;
        }
        Console.WriteLine($"{anc}|{response}");
        // 假设模型返回的信息是正确的
        if (response.Contains(anc))
        {
            accuracy = 1.0f;
            return true;
        }
        else
        {
            accuracy = 0.0f;
            return false;
        }
    }

}
