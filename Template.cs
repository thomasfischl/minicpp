// Template.cs:                                       HDO, 1998-2005, 2006-08-28
// -----------
//
// Class Template handles template text files consisting of fixed
// text and named place holders in the form $name$.
//=====================================|========================================

#define TEMPLATE

#if TEMPLATE

#undef TEST_TEMPLATE

using System;
using System.Collections;
using System.IO;
using System.Text;


public class Template {

  private String    templateFileName = null; // name of template file 
  private DateTime  lastMod;                 // date of its last modification 
  private ArrayList parts;                   // container for objects of class Part

  private class Part {
    private String str; // string for text or a place holder
    protected Part(String str) {
      this.str = str;
    } // Part
    override public String ToString() {
      return str;
    } // toString
  } // Part
  
  private class Text: Part {
    public Text(String text)
    : base (text) {
      // nothing to do
    } // Text
  } // Text
  
  private class PlaceHolder: Part {
    public PlaceHolder(String placeHolder)
    : base(placeHolder) {
      // nothing to do
    } // PlaceHolder
  } // PlaceHolder


  public class InstantiationException: Exception {
    public InstantiationException(String s)
    : base(s) {
      // nothing to do
    } // InstantiationException
  } // InstantiationException


  public Template(String path, String templateFileName) {
  //-----------------------------------|----------------------------------------
    this.templateFileName = path + templateFileName;
    try {
      lastMod = File.GetLastWriteTime(templateFileName);
      ParseTemplateFile();
    } catch (Exception) {
      throw new InstantiationException("template file \"" + templateFileName + 
                                       "\" cannot be found/parsed");
    } // catch
  } // Template


  public void ParseTemplateFile() {
  
    StreamReader   sr     = new StreamReader(File.OpenRead(templateFileName));
    StringBuilder  sb     = new StringBuilder();
    int            intCh  = -1;
    char           ch     = ' ';
    bool           inText = true;

    parts =  new ArrayList();
    while (true) {
      intCh =  sr.Read();
      if ( (intCh == -1) || 
           ((ch = (char)intCh) == '$') ) {
        if (inText)
          parts.Add(new Text(       sb.ToString()));
        else
          parts.Add(new PlaceHolder(sb.ToString()));
        if (intCh == -1)
          break;
        else { // ch == '$'
          inText = !inText;
          sb = new StringBuilder();
          continue; // skip '$'
        } // else
      } // if
      sb.Append(ch);
    } // while
    
  } // ParseTemplateFile


  public String Instance(String[] placeHolders, String[] replacements) {
  //-----------------------------------|----------------------------------------

    if (placeHolders.Length != replacements.Length)
      throw new InstantiationException(
                  "nonmatching number of place holders and replacements");

    DateTime newLm = File.GetLastWriteTime(templateFileName);
    if (!lastMod.Equals(newLm)) {
      lastMod = newLm;
      try {
        /*Re*/ParseTemplateFile();
      } catch (Exception) {
        throw new InstantiationException("new template file cannot be reparsed");
      } // catch
    } // if

    Hashtable ht = new Hashtable(placeHolders.Length * 2);
    for (int i = 0; i < placeHolders.Length; i++) {
      if (placeHolders[i] == null)
        throw new InstantiationException(
                    "placeholder at index " + i + " is null");
      if (replacements[i] == null)
        throw new InstantiationException(
                    "replacement for placeholder " +
                    placeHolders[i] + " is null");
      ht.Add(placeHolders[i], replacements[i]);
    } // for

    StringBuilder sb = new StringBuilder();
    foreach (Part p in parts) {
      if (p is Text)
        sb.Append(p.ToString());
      else { // p is PlaceHolder
        String replacement = (String)ht[p.ToString()];
        if (replacement != null)
          sb.Append(replacement);
        else // use place holder instead of replacement
          sb.Append("$" + p.ToString() + "$");
      } // else
    } // while
    return sb.ToString();

  } // Instance


#if TEST_TEMPLATE

  // for test and demonstration purposes only

  public String SequenceOfParts() {
    StringBuilder sb = new StringBuilder();
    foreach (Part p in parts) {
      if (p.GetType() == typeof(Text)) {
        sb.Append("text:         ");
        String text = p.ToString();
        if (text.Length <= 10)
          {} // text = text/*.replace('\n', '#')*/;
        else
          text = text.Substring(0, 10) + " ... " +
                 text.Substring(text.Length - 10);
        sb.Append(text);
      } else  // p.GetType() == typeof(PlaceHolder)
        sb.Append("place holder: " + "$" + p.ToString() + "$");
      sb.Append("\n");
    } // while
    return sb.ToString();
  } // SequenceOfParts

  public static void Main(String[] args) {
    Console.WriteLine("START: Template");
    try {
    
      Template t = new Template("HelloWorldTemplate.html");
      
      Console.WriteLine();
      Console.WriteLine("structure of the template (sequence of parts):");
      Console.WriteLine("---------------------------------------------");
      Console.WriteLine(t.SequenceOfParts());
      
      Console.WriteLine();
      Console.WriteLine("example of an instantiation:");
      Console.WriteLine("---------------------------");
      Console.WriteLine(t.Instance(
        new String[]{"first", "second", "title",      "body"},
        new String[]{"A",     "B",      "HelloWorld", "Hello, World!"}));
                   
    } catch (Exception e) {
      Console.WriteLine(e);
    } // catch

    Console.WriteLine("END");
    Console.WriteLine();
    Console.WriteLine("type [CR] to continue ...");
    Console.ReadLine();

  } // main

#endif // TEST_TEMPLATE

} // Template

#endif // TEMPLATE

// End of Template.cs
//=====================================|========================================