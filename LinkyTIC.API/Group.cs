using System;
using System.ComponentModel;
using System.Reflection;

namespace LinkyTIC.API
{
    public class NewGroup
    {
        public string Label { get; private set; } = "";

        public string Horodate { get; private set; } = "";

        public string Data { get; private set; } = "";

        public byte Checksum { get; private set; } = 0;

        private List<byte> raw = new List<byte>();

        internal enum ParsingByte
        {
            Label,
            Horodate,
            Data,
            Checksum,
            EndOfComponent
        }

        internal bool ResolveLabel(out GroupLabels result)
        {
            if (GroupLabelsExtension.AllLabels.ContainsKey(Label))
            {
                result = GroupLabelsExtension.AllLabels[Label];
                return true;
            }
            result = GroupLabels.ADSC;
            return false;
        }

        internal void Append(byte b, ParsingByte t)
        {
            switch (t)
            {
                case ParsingByte.Label:
                    Label += Convert.ToChar(b);
                    raw.Add(b);
                    break;
                case ParsingByte.Horodate:
                    Horodate += Convert.ToChar(b);
                    raw.Add(b);
                    break;
                case ParsingByte.Data:
                    Data += Convert.ToChar(b);
                    raw.Add(b);
                    break;
                case ParsingByte.Checksum:
                    Checksum = b;
                    break;
                case ParsingByte.EndOfComponent:
                    raw.Add(b);
                    break;
            }
        }

        public bool IsValid
        {
            get
            {
                var s1 = raw.Sum(_ => Convert.ToInt32(_));
                var s2 = s1 & 0x3F;
                var checksum = s2 + 0x20;
                return checksum == Checksum;
            }
        }

        public GroupValue ToGroupValue()
        {
            return new GroupValue
            {
                DateTime = string.IsNullOrEmpty(Horodate) ? null : DateTime.ParseExact(Horodate.Substring(1), "yyMMddHHmmss", null),
                Value = Data
            };
        }
    }

    public class GroupValue
    {
        public DateTime? DateTime { get; internal set; }

        public string Value { get; internal set; } = "";

        public int IntValue { get => int.Parse(Value); }
    }

    public enum GroupLabels
    {
        [Label("Adresse Secondaire du Compteur", false)]
        ADSC,

        [Label("Version de la TIC", false)]
        VTIC,

        [Label("Date et heure courante", true)]
        DATE,

        [Label("Nom du calendrier tarifaire fournisseur", false)]
        NGTF,

        [Label("Libellé tarif fournisseur en cours", false)]
        LTARF,

        [Label("Energie active soutirée totale", false, "Wh")]
        EAST,

        [Label("Energie active soutirée Fournisseur, index 01", false, "Wh")]
        EASF01,

        [Label("Energie active soutirée Fournisseur, index 02", false, "Wh")]
        EASF02,

        [Label("Energie active soutirée Fournisseur, index 03", false, "Wh")]
        EASF03,

        [Label("Energie active soutirée Fournisseur, index 04", false, "Wh")]
        EASF04,

        [Label("Energie active soutirée Fournisseur, index 05", false, "Wh")]
        EASF05,

        [Label("Energie active soutirée Fournisseur, index 06", false, "Wh")]
        EASF06,

        [Label("Energie active soutirée Fournisseur, index 07", false, "Wh")]
        EASF07,

        [Label("Energie active soutirée Fournisseur, index 08", false, "Wh")]
        EASF08,

        [Label("Energie active soutirée Fournisseur, index 09", false, "Wh")]
        EASF09,

        [Label("Energie active soutirée Fournisseur, index 10", false, "Wh")]
        EASF10,

        [Label("Energie active soutirée Distributeur, index 01", false, "Wh")]
        EASD01,

        [Label("Energie active soutirée Distributeur, index 02", false, "Wh")]
        EASD02,

        [Label("Energie active soutirée Distributeur, index 03", false, "Wh")]
        EASD03,

        [Label("Energie active soutirée Distributeur, index 04", false, "Wh")]
        EASD04,

        [Label("Courant efficace, phase 1", false, "A")]
        IRMS1,

        [Label("Tension efficace, phase 1", false, "V")]
        URMS1,

        [Label("Puissance app. de référence (PREF)", false, "kVA")]
        PREF,

        [Label("Puissance app. de coupure (PCOUP)", false, "kVA")]
        PCOUP,

        [Label("Puissance app. Instantanée soutirée", false, "VA")]
        SINSTS,

        [Label("Puissance app. max. soutirée n", true, "VA")]
        SMAXSN,

        [Label("Puissance app. max. soutirée n-1", true, "VA", KeyOverride = "SMAXSN-1")]
        SMAXSN_1,

        [Label("Point n de la courbe de charge active soutirée", true, "W")]
        CCASN,

        [Label("Point n-1 de la courbe de charge active soutirée", true, "W", KeyOverride = "CCASN-1")]
        CCASN_1,

        [Label("Tension moy. ph. 1", true, "V")]
        UMOY1,

        [Label("Registre de Statuts", false)]
        STGE,

        [Label("Début Pointe Mobile 1", true)]
        DPM1,

        [Label("Fin Pointe Mobile 1", true)]
        FPM1,

        [Label("Début Pointe Mobile 2", true)]
        DPM2,

        [Label("Fin Pointe Mobile 2", true)]
        FPM2,

        [Label("Début Pointe Mobile 3", true)]
        DPM3,

        [Label("Fin Pointe Mobile 3", true)]
        FPM3,

        [Label("Message court", false)]
        MSG1,

        [Label("Message Ultra court", false)]
        MSG2,

        [Label("PRM", false)]
        PRM,

        [Label("Relais", false)]
        RELAIS,

        [Label("Numéro de l’index tarifaire en cours", false)]
        NTARF,

        [Label("Numéro du jour en cours calendrier fournisseur", false)]
        NJOURF,

        [Label("Numéro du prochain jour calendrier fournisseur", false, KeyOverride = "NJOURF+1")]
        NJOURF_1,

        [Label("Profil du prochain jour calendrier fournisseur", false, KeyOverride = "PJOURF+1")]
        PJOURF_1,

        [Label("Profil du prochain jour de pointe", false)]
        PPOINTE,
    }

    public static class GroupLabelsExtension
    {
        internal static Dictionary<string, GroupLabels> AllLabels = Enum.GetValues<GroupLabels>().ToDictionary(_ => _.GetKey(), _ => _);

        public static string GetKey(this GroupLabels label) => label.GetInfo().KeyOverride ?? label.ToString();

        public static LabelAttribute GetInfo(this GroupLabels label)
        {
            FieldInfo fi = label.GetType().GetField(label.ToString())!;
            LabelAttribute[] attributes = (LabelAttribute[])fi.GetCustomAttributes<LabelAttribute>(false);
            return attributes.First();
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class LabelAttribute : Attribute
    {
        public string Title { get; set; }

        public bool HasHorodate { get; set; }

        public string? Unit { get; set; }

        public string Comments { get; set; }

        public string? KeyOverride { get; set; }

        public LabelAttribute(string title, bool hasHorodate, string? unit = null, string comments = "")
        {
            Title = title;
            HasHorodate = hasHorodate;
            Unit = unit;
            Comments = comments;
        }
    }
}

