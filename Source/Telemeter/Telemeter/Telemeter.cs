using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;

using System.Net;
using System.Net.Sockets;

static class Extensions
{
    public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> items, T separator)
    // credit where credit is due: this is from Mike Hadlow at
    // http://mikehadlow.blogspot.com/2012/04/useful-linq-extension-method.html
    // -- I daresay I couldn't have written it any simpler myself.
    {
        var first = true;
        foreach (var item in items)
        {
            if (first) first = false;
            else
            {
                yield return separator;
            }
            yield return item;
        }
    }


}

public class Telemeter : Part
{
    // * Part callbacks
    private delegate String DamnYouMono(Field field);
    protected override void onPartUpdate()
    {
        // Oh, LINQ, how I love thee / for thine functionality

        DamnYouMono printField = (Field field) =>
        {
            switch (field)
            {
                case Field.NaN:
                    return "NaN";
                case Field.MET:
                    return this.vessel.missionTime.ToString();
                case Field.vName:
                    return this.vessel.name;
                default:
                    return "NaN";
            }
        };

        Send(printField(RowFields.First()));
        foreach (Field field in RowFields.Skip(1))
        {
            Send(",");
            Send(printField(field));
        }

        Send("\n");
    }

    // * Managing the global socket
    
    // Our attempt at solving the "when do the close the socket" problem - by never
    // doing it. From the point of the client, we can't distinguish between seperate
    // connections, anyway.
    private static PluginConfiguration Settings = PluginConfiguration.CreateForType<Telemeter>();
    private static UdpClient Udp;
    private static List<Field> RowFields = new List<Field>();
    static Telemeter()
    {
        Settings.load();

        Udp = new UdpClient();
        Udp.Connect(new IPEndPoint(IPAddress.Parse(Settings.GetValue<string>("ip")), Settings.GetValue<int>("port")));

        string[] rawFields = Settings.GetValue<string>("format").Split(new char[] {' '});
        foreach (string rawField in rawFields)
        {
            try
            {
                RowFields.Add((Field)Enum.Parse(typeof(Field), rawField));
            }
            catch (ArgumentException)
            {
                RowFields.Add(Field.NaN);
            }
        }
    }
    private static void Send(string str)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(str);
        Udp.Send(bytes, bytes.Length);
    }

    private enum Field
    {
        NaN, MET, vName
    }
}