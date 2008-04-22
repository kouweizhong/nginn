<?xml version="1.0" encoding="utf-8" ?>
<!-- this is a stylesheet for converting 
	 process definition into dot.exe graph description file.
	 It is used for generating process graphical representation.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns="http://www.nginn.org/WorkflowDefinition.1_0.xsd" xmlns:ng="http://www.nginn.org/WorkflowDefinition.1_0.xsd">
	<xsl:output method="text" indent="yes" />
	<xsl:template match="/">
		<xsl:apply-templates select="ng:process" />
	</xsl:template>
	
	<xsl:template match="ng:process">
		digraph <xsl:value-of select="@name" />_<xsl:value-of select="@version" /> {
			<xsl:apply-templates select="ng:places" />
			<xsl:apply-templates select="ng:tasks" />
			<xsl:apply-templates select="ng:flows" />
		}
	</xsl:template>
	
	<xsl:template match="ng:places">
		//places
		<xsl:apply-templates select="*" />
	</xsl:template>
	<xsl:template match="ng:tasks">
		//tasks
		<xsl:apply-templates select="*" />
	</xsl:template>
	<xsl:template match="ng:flows">
		//flows
		<xsl:apply-templates select="*" />
	</xsl:template>
	
	<xsl:template match="ng:place[@type='StartPlace']">
		<xsl:value-of select="@id" /> [shape=circle, style=filled, fillcolor=orange, peripheries=2];
	</xsl:template>
	
	<xsl:template match="ng:place[@type='EndPlace']">
		<xsl:value-of select="@id" /> [shape=circle, style=filled, fillcolor=lightgray, peripheries=2,style=bold];
	</xsl:template>
	
	<xsl:template match="ng:place">
		<xsl:value-of select="@id" /> [shape=circle, style=filled, fillcolor=lightblue, peripheries=1];
	</xsl:template>
	
	
	
	
	<xsl:template match="ng:task">
		<xsl:value-of select="@id" /> [shape=box, style=filled, fillcolor=yellow, color=black, peripheries=1];
	</xsl:template>
	
	<xsl:template match="ng:flow">
		<xsl:variable name="fromid" select="ng:from/text()" />
		<xsl:variable name="toid" select="ng:to/text()" />
		<xsl:variable name="fromnode" select="//ng:*[@id=$fromid]" />
		<xsl:variable name="tonode" select="//ng:*[@id=$toid]" />
		<xsl:value-of select="ng:from" /> -&gt; <xsl:value-of select="ng:to" /> [
			<xsl:choose>
				<xsl:when test="$fromnode/ng:splitType = 'XOR'">arrowtail=diamond</xsl:when>
				<xsl:when test="$fromnode/ng:splitType = 'OR'">arrowtail=odiamond</xsl:when>
				<xsl:otherwise>arrowtail=none</xsl:otherwise>
			</xsl:choose>,
			<xsl:choose>
				<xsl:when test="$tonode/ng:joinType = 'XOR'">arrowhead=open</xsl:when>
				<xsl:when test="$tonode/ng:joinType = 'OR'">arrowhead=empty</xsl:when>
				<xsl:otherwise>arrowhead=normal</xsl:otherwise>
			</xsl:choose>
			];
	</xsl:template>
</xsl:stylesheet>