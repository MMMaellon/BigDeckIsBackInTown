
using System;
using Iwashi.UdonTask;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
#endif

namespace MMMaellon.BigDeckIsBackInTown
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CardTextBank : UdonSharpBehaviour
    {
        public CardText[] cards;
        [Header("Will \"reshuffle\" this many times when picking a card. Should probably be around 3.")]
        public int reshuffles = 3;
        public string loading_text = "Loading...";
        [NonSerialized]
        public string[] texts = new string[0];
        [NonSerialized]
        public string[] bank_names = new string[0];
        [NonSerialized]
        public int[] starts = { 0 };
        [NonSerialized]
        public int[] lengths = { 0 };
        [NonSerialized, UdonSynced, FieldChangeCallback(nameof(active))]
        public bool[] _active = { };
        public bool[] active
        {
            get => _active;
            set
            {
                _active = value;
                CalcActiveBanks();
                if (!parse_callback_called)
                {
                    ParseCallback();
                }
            }
        }
        bool parse_callback_called = false;
        public string random_variable = "[RANDOM_NAME]";
        public string player_variable = "[PLAYER_NAME]";
        public string name_opening_tag = "<b>";
        public string name_closing_tag = "</b>";
        public string ReplaceStringVariables(string input_str, VRCPlayerApi player, VRCPlayerApi random)
        {
            if (random_variable.Length != 0 && Utilities.IsValid(random))
            {
                input_str = input_str.Replace(random_variable, name_opening_tag + random.displayName + name_closing_tag);
            }
            if (player_variable.Length != 0 && Utilities.IsValid(player))
            {
                input_str = input_str.Replace(player_variable, name_opening_tag + player.displayName + name_closing_tag);
            }
            return input_str;
        }

        [NonSerialized]
        public int total_active_length;
        public void Clear()
        {
            texts = new string[0];
            starts = new int[0];
            lengths = new int[0];
            active = new bool[0];
            total_active_length = 0;
        }

        public int retries = 3;
        int retry_count = 0;
        [Header("Will try URL first unless it's blank. Falls back to the text file")]
        public VRCUrl url = new VRCUrl("https://gist.githubusercontent.com/MMMaellon/389fc91566655506f14e8571cf279ad5/raw/7566b25ad4bc526f29438d064e1a122ba1a7b9ae/example_bank.json");
        public TextAsset text_file;
        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            Debug.LogWarning(gameObject.name + " Loading URL SUCCESS");
            Debug.LogWarning("We're about to try to parse some JSON from " + url.ToString());
            AsyncParse(result.Result);
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.LogWarning(gameObject.name + " Loading URL FAILED");
            if (retry_count >= retries)
            {
                //FUCK
                LoadFile();
                return;
            }
            SendCustomEventDelayedSeconds(nameof(LoadURL), 10);
            retry_count++;
        }
        public void Start()
        {
            if (url.ToString() == "")
            {
                LoadFile();
            }
            else
            {
                LoadURL();
            }
        }

        public void LoadFile()
        {
            if (text_file)
            {
                AsyncParse(text_file.text);
            }
        }

        public void LoadURL()
        {
            Debug.LogWarning(gameObject.name + " Loading URL");
            VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        }
        float start_time;
        public void AsyncParse(string raw)
        {
            start_time = Time.timeSinceLevelLoad;
            UdonTask.New((IUdonEventReceiver)this, nameof(ParseJSON), nameof(OnParseComplete), "input", "output", raw);
            // ParseSync(raw);

        }

        public UdonTaskContainer ParseJSON(UdonTaskContainer input)
        {
            Debug.LogWarning("Parse Start");
            UdonTaskContainer container = UdonTaskContainer.New();
            string json = input.GetVariable<string>(0);
            DataDictionary dict;
            DataList text_list = new DataList();
            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                dict = result.DataDictionary;
                var keys = dict.GetKeys();
                string[] texts;
                string[] bank_names = new string[keys.Count];
                int[] starts = new int[keys.Count];
                int[] lengths = new int[keys.Count];
                for (int i = 0; i < keys.Count; i++)
                {
                    bank_names[i] = keys[i].String;
                    starts[i] = text_list.Count;
                    if (dict[keys[i]].TokenType == TokenType.DataList)
                    {
                        foreach (var token in dict[keys[i]].DataList.ToArray())
                        {
                            text_list.Add(token.String);
                        }
                    }
                    lengths[i] = text_list.Count - starts[i];
                }
                texts = new string[text_list.Count];
                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = text_list[i].String;
                }
                //this is apparently how you add a variable
                container = container.AddVariable(texts);
                container = container.AddVariable(bank_names);
                container = container.AddVariable(starts);
                container = container.AddVariable(lengths);
            }
            else
            {
                Debug.LogError("Invalid JSON: " + result.ToString());
            }
            return container;
        }

        public void ParseSync(string json)
        {
            DataDictionary dict;
            DataList text_list = new DataList();
            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                dict = result.DataDictionary;
                var keys = dict.GetKeys();
                bank_names = new string[keys.Count];
                starts = new int[keys.Count];
                lengths = new int[keys.Count];
                for (int i = 0; i < keys.Count; i++)
                {
                    bank_names[i] = keys[i].String;
                    starts[i] = text_list.Count;
                    if (dict[keys[i]].TokenType == TokenType.DataList)
                    {
                        foreach (var token in dict[keys[i]].DataList.ToArray())
                        {
                            text_list.Add(token.String);
                        }
                    }
                    lengths[i] = text_list.Count - starts[i];
                }
                texts = new string[text_list.Count];
                for (int i = 0; i < texts.Length; i++)
                {
                    texts[i] = text_list[i].String;
                }
                OnParseCompleteSync();
            }
            else
            {
                Debug.LogError("Invalid JSON: " + result.ToString());
            }
        }

        public bool IsParsing()
        {
            return !parsed && retry_count < retries;
        }

        bool parsed = false;
        public void OnParseComplete(UdonTaskContainer output)
        {
            texts = output.GetVariable<string[]>(0);
            bank_names = output.GetVariable<string[]>(1);
            starts = output.GetVariable<int[]>(2);
            lengths = output.GetVariable<int[]>(3);
            OnParseCompleteSync();
        }

        public void OnParseCompleteSync()
        {
            parsed = true;
            if (active == null || active.Length == 0)
            {
                active = new bool[bank_names.Length];
                for (int i = 0; i < active.Length; i++)
                {
                    active[i] = true;
                }
                RequestSerialization();
            }
            CalcActiveBanks();
            Debug.LogWarning("Completed parse in: " + (Time.timeSinceLevelLoad - start_time));
            ParseCallback();
        }
        public void ParseCallback()
        {
            parse_callback_called = true;
            foreach (CardText card in cards)
            {
                card.bank = this;
                card.OnBankParseComplete();
            }
        }

        public void CalcActiveBanks()
        {
            if (!parsed)
            {
                return;
            }
            total_active_length = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (i < active.Length && active[i])
                {
                    total_active_length += lengths[i];
                }
            }
        }

        public override void OnDeserialization()
        {
            CalcActiveBanks();
        }

        [System.NonSerialized]
        public DataDictionary last_text_selection_times = new DataDictionary();
        [System.NonSerialized]
        public DataDictionary last_player_selection_times = new DataDictionary();
        int next_random_text_id;
        int next_random_player_id;
        int most_random_text_id;
        int most_random_player_id;

        public bool limit_player_name_selection_by_distance = true;
        public float max_distance = 10f;
        DataList player_list = new DataList();
        VRCPlayerApi[] players;
        VRCPlayerApi random_player;
        public int RandomPlayerId()
        {
            players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            if (!limit_player_name_selection_by_distance)
            {
                random_player = players[UnityEngine.Random.Range(0, players.Length)];
                if (Utilities.IsValid(random_player))
                {
                    return random_player.playerId;
                }
                else
                {
                    //I don't wanna get stuck in a retry loop so just return ourself
                    return Networking.LocalPlayer.playerId;
                }
            }
            player_list.Clear();
            foreach (var player in players)
            {
                if (!Utilities.IsValid(player) || (Vector3.Distance(transform.position, player.GetPosition()) > max_distance))
                {
                    continue;
                }
                player_list.Add(player.playerId);
            }
            if (player_list.Count == 0)
            {
                last_player_selection_times[Networking.LocalPlayer.playerId] = Time.timeSinceLevelLoad;
                return Networking.LocalPlayer.playerId;
            }

            //select a bunch and choose the one that wasn't selected recently
            most_random_player_id = UnityEngine.Random.Range(0, player_list.Count);
            if (reshuffles > 0)
            {
                for (int i = 0; i < reshuffles; i++)
                {
                    next_random_player_id = UnityEngine.Random.Range(0, player_list.Count);
                    if (!last_player_selection_times.ContainsKey(most_random_player_id))
                    {
                        break;
                    }
                    if (!last_player_selection_times.ContainsKey(next_random_player_id))
                    {
                        most_random_player_id = next_random_player_id;
                        break;
                    }
                    if (last_player_selection_times[next_random_player_id].Float < last_player_selection_times[most_random_player_id].Float)
                    {
                        most_random_player_id = next_random_player_id;
                    }
                }
            }

            return player_list[most_random_player_id].Int;
        }

        int random_card;
        int skipped_cards;
        int counted_cards;
        public int RandomCardId()
        {
            if (total_active_length <= 0)
            {
                return -1001;
            }

            //select a bunch and choose the one that wasn't selected recently
            most_random_text_id = UnityEngine.Random.Range(0, total_active_length);
            if (reshuffles > 0)
            {
                for (int i = 0; i < reshuffles; i++)
                {
                    next_random_text_id = UnityEngine.Random.Range(0, player_list.Count);
                    if (!last_text_selection_times.ContainsKey(most_random_text_id))
                    {
                        break;
                    }
                    if (!last_text_selection_times.ContainsKey(next_random_text_id))
                    {
                        most_random_text_id = next_random_text_id;
                        break;
                    }
                    if (last_text_selection_times[next_random_text_id].Float < last_text_selection_times[most_random_text_id].Float)
                    {
                        most_random_text_id = next_random_text_id;
                    }
                }
            }

            random_card = most_random_text_id;

            skipped_cards = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                if (!active[i])
                {
                    skipped_cards += lengths[i];
                    continue;
                }
                else
                {
                    counted_cards += lengths[i];
                }
                if (random_card < counted_cards)
                {
                    random_card = skipped_cards + random_card;
                }
            }

            if (random_card < 0 || random_card >= texts.Length)
            {
                return -1001;
            }

            return random_card;
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public void OnValidate()
        {

            if (cards.Length > 0)
            {
                HashSet<CardText> valid_cards = new HashSet<CardText>();

                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i])
                    {
                        valid_cards.Add(cards[i]);
                    }
                }
                cards = valid_cards.ToArray<CardText>();
            }
            else
            {
                cards = GetComponentsInChildren<CardText>();
            }
        }
#endif
    }
}
