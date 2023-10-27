using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Api.CloudHosts;


/// <summary>
/// A class which represents a "context" in NGINX config lingo.
/// A context is what you might call a block - it has a list of directives and can potentially have blocks nested inside it too.
///
///  
/// location / {
///	
/// }
/// ^ a location context
/// "location /" is the directive of the above context.
/// The list of directives (inside the brackets) in this example is empty.
/// </summary>
public partial class NGINXContext
{
	/// <summary>
	/// The directive of the context itself.
	/// </summary>
	public NGINXDirective Directive;
	
	/// <summary>
	/// The list of contexts within this context. NGINX context (blocks) are nestable.
	/// </summary>
	public List<NGINXContext> Contexts;
	
	/// <summary>
	/// The classic key/value pairing which defines a particular config value is called a directive.
	/// It is a list rather than a dictionary because for a variety of directives, order matters.
	/// </summary>
	public List<NGINXDirective> Directives;
	
	
	/// <summary>
	/// Adds a location context as a child of this one.
	/// A location context is e.g. "location / { ...}" in the config file.
	/// </summary>
	public NGINXContext AddLocationContext(string locationRule)
	{
		return AddContext("location", locationRule);
	}
	
	/// <summary>
	/// Add a context of the given type as a child. Returns the new context. See also: AddLocationContext.
	/// Returns *the child context*.
	/// </summary>
	public NGINXContext AddContext(string contextType, string value = null)
	{
		var ctx = new NGINXContext();
		ctx.Directive = new NGINXDirective(contextType, value);
		AddContext(ctx);
		return ctx;
	}
	
	/// <summary>
	/// Adds the given context to this one as a child context.
	/// Returns *the child context*.
	/// </summary>
	public NGINXContext AddContext(NGINXContext context)
	{
		if(Contexts == null)
		{
			Contexts = new List<NGINXContext>();
		}
		
		Contexts.Add(context);
		return context;
	}
	
	/// <summary>
	/// Adds the given directive (key/value config pair) to this one as a child.
	/// Returns this context for easy chaining.
	/// Like this:
	/// 
	///  nginxConfigFile.AddDirective("error_log", "/var/log/nginx/error.log").AddDirective("server_name", "www.site.com")...
	/// 
	/// If you need the actual NGINXDirective object, use the other AddDirective.
	/// </summary>
	public NGINXContext AddDirective(string key, string value)
	{
		var dir = new NGINXDirective(key, value);
		AddDirective(dir);
		// Returning this context allows AddDirective to chain.
		// Handy for something like this where there is usually a list of config entries that 
		// need to be added and you (probably) don't care about the resulting object.
		return this;
	}
	
	/// <summary>
	/// Adds the given directive (key/value config pair) to this one as a child.
	/// </summary>
	public NGINXContext AddDirective(NGINXDirective directive)
	{
		if(Directives == null)
		{
			Directives = new List<NGINXDirective>();
		}
		
		Directives.Add(directive);
		return this;
	}
	
	
	/// <summary>
	/// Generates the textual configuration of this context.
	/// </summary>
	public override string ToString()
	{
		var sb = new StringBuilder();
		ToString(sb, 0);
		return sb.ToString();
	}
	
	/// <summary>
	/// Efficient mechanism for stringifying this block.
	/// </summary>
	public void ToString(StringBuilder builder, int depth = 0)
	{
		// Directive is null for the config file itself. It is an NGINX context with no directive.
		
		if(Directive != null)
		{
			Indent(builder, depth);
			Directive.ToString(builder);
			builder.Append(" {");
			depth++;
		}
		
		// Convention means child directives appear first, followed by child contexts.
		if(Directives != null)
		{
			
			for(var i=0;i<Directives.Count;i++)
			{
				// Add the newline between directives, and between the block start and the first directive:
				builder.Append("\r\n");
				
				// Indent the current directive line:
				Indent(builder, depth);
				
				// Write the directive out:
				Directives[i].ToString(builder);
				
				// And in this scenario we add ; on the end of it:
				builder.Append(';');
				
			}
			
		}
		
		if(Contexts != null && Contexts.Count > 0)
		{
			if(Directives != null && Directives.Count > 0)
			{
				// Add another newline between the directives and the contexts set.
				builder.Append("\r\n");
				Indent(builder, depth);
				builder.Append("\r\n");
				
			}
			
			for(var i=0;i<Contexts.Count;i++)
			{
				// Add a double newline between contexts:
				builder.Append("\r\n\r\n");
				
				// Writing the context will internally indent itself.
				
				// Write the context out:
				Contexts[i].ToString(builder, depth);
			}
		}
		
		if(Directive != null)
		{
			depth--;
			builder.Append("\r\n");
			Indent(builder, depth);
			builder.Append("}");
		}
	}
	
	/// <summary>
	/// Adds tabs to the given builder based on the current indent depth.
	/// I.e. depth 0 is 0 tabs added, 1 is 1 tab added etc.
	/// </summary>
	private void Indent(StringBuilder builder, int depth){
		if(depth == 0)
		{
			// No tabs to add
			return;
		}
		
		// Add the tabs
		for(var i=0;i<depth;i++){
			builder.Append('\t');
		}
		
	}
	
}