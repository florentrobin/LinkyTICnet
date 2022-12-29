using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace LinkyTIC.API
{
    public class Frame : IReadOnlyDictionary<GroupLabels, GroupValue?>
    {
        private Dictionary<GroupLabels, GroupValue> _dict = new Dictionary<GroupLabels, GroupValue>();

        public const byte StartTeXt = 0x02;
        public const byte EndTeXt = 0x03;

        private const byte GroupStart = 0x0A;
        private const byte GroupEnd = 0x0D;
        private const byte GroupSep = 0x09;

        public IEnumerable<GroupLabels> Keys { get => _dict.Keys; }

        public IEnumerable<GroupValue> Values {get => _dict.Values; }

        public int Count { get => _dict.Keys.Count; }

        public GroupValue? this[GroupLabels key] { get => _dict.ContainsKey(key) ? _dict [key] : null; }

        public Frame(List<byte> frameData, ILogger? logger = null)
        {
            if (frameData.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameData), "The frame data cannot be empty");
            }

            NewGroup? newGroup = null;
            NewGroup.ParsingByte parsingState = NewGroup.ParsingByte.Label;

            foreach (var b in frameData)
            {
                switch (b)
                {
                    case Frame.StartTeXt:
                    case Frame.EndTeXt:
                        break;
                    case Frame.GroupStart:
                        newGroup = new NewGroup();
                        parsingState = NewGroup.ParsingByte.Label;
                        break;
                    case Frame.GroupSep:
                        if (newGroup != null)
                        {
                            switch (parsingState)
                            {
                                case NewGroup.ParsingByte.Label:
                                    if (newGroup.ResolveLabel(out GroupLabels label))
                                    {
                                        if (label.GetInfo().HasHorodate)
                                        {
                                            parsingState = NewGroup.ParsingByte.Horodate;
                                        }
                                        else
                                        {
                                            parsingState = NewGroup.ParsingByte.Data;
                                        }
                                    }
                                    else
                                    {
                                        logger?.LogError("Unknown label: {label}. Data={data}", newGroup.Label, newGroup.Data);
                                        newGroup = null;
                                    }
                                    break;
                                case NewGroup.ParsingByte.Horodate:
                                    parsingState = NewGroup.ParsingByte.Data;
                                    break;
                                case NewGroup.ParsingByte.Data:
                                    parsingState = NewGroup.ParsingByte.Checksum;
                                    break;
                                default:
                                    break;
                            }
                            newGroup?.Append(b, NewGroup.ParsingByte.EndOfComponent);
                        }
                        break;
                    case Frame.GroupEnd:
                        if (newGroup != null)
                        {
                            if (newGroup.ResolveLabel(out GroupLabels label))
                            {
                                _dict.Add(label, newGroup.ToGroupValue());
                                newGroup = null;
                            }
                            else
                            {
                                logger?.LogError("Unknown label: {label}. Data={data}", newGroup.Label, newGroup.Data);
                            }
                        }
                        break;
                    default:
                        if (newGroup != null)
                        {
                            newGroup.Append(b, parsingState);
                        }
                        break;
                }
            }
        }

        public bool ContainsKey(GroupLabels key) => _dict.ContainsKey(key);

        public bool TryGetValue(GroupLabels key, [MaybeNullWhen(false)] out GroupValue value) => _dict.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<GroupLabels, GroupValue?>> GetEnumerator() => _dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}

