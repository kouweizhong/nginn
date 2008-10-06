<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:output method="html" indent="yes"/>

  <xsl:template match="/">
    <html>
      <head>
        <title>Task details</title>
        <style>
          td.label {
          background: #e0e0ef;
          padding: 8px 2px 2px 2px;
          margin: 0 0 0 10;
          }
          td.field {
          background: #fff0f0;
          border: solid 1px black;
          padding: 2px 2px 2px 2px;
          margin: 0 0 0 0;
          }
          td.field2 {
          background: #fff0f0;
          margin: 0 0 0 0;
          }

          input.field_ro {
          border: none;
          readonly: true;
          border: solid 1px #a0a0a0;
          width:100%;
          }
          input.field_rw {
          border: solid 1px #a0a0a0;
          border: none;
          width:100%;
          }
          input.field_rq {
          border: solid 1px red;
          background:yellow;
          width:100%;
          }
        </style>
      </head>
      <body>
        <xsl:apply-templates select="*" />
      </body>
    </html>
  </xsl:template>

  <xsl:template match="Task">
    <div>
      <input type="button" name="btn_complete" value="Complete task" onclick="javascript:document.getElementById('taskDetails').submit()"/>
      &#160;
      <xsl:if test="Status = '2'">
        <input type="button" name="btn_select" value="Start execution">
          <xsl:attribute name="onclick">
            javascript:document.location.href = 'SelectTask.aspx?correlationId=<xsl:value-of select="CorrelationId"/>'
          </xsl:attribute>
        </input>
      </xsl:if>
      &#160;
      <input type="button" name="btn_back" value="Exit" onclick="javascript:history.back()"/>
    </div>
    <form name="taskDetails" id="taskDetails" method="POST">
      <xsl:attribute name="action">CompleteTask.aspx?correlationId=<xsl:value-of select="CorrelationId"/>
      </xsl:attribute>
    <table cellpadding="0" cellspacing="0" >
      <col width="25%" />
      <col width="25%" />
      <col width="25%" />
      <col width="25%" />
      <tr>
        <td class="label">Id</td>
        <td class="label">Status</td>
        <td class="label">Assignee</td>
        <td class="label">Assignee group</td>
      </tr>
      <tr>
        <td class="field">
          <xsl:value-of select="Id"/>
        </td>
        <td class="field">
          <xsl:value-of select="StatusName"/>
        </td>
        <td class="field">
          <xsl:value-of select="Assignee"/>
        </td>
        <td class="field">
          <xsl:value-of select="AssigneeGroup"/>
        </td>
      </tr>
      <tr>
        <td class="label">TaskId</td>
        <td class="label">Created date</td>
        <td class="label">Execution start</td>
        <td class="label">Execution end</td>
      </tr>
      <tr>
        <td class="field">
          <xsl:value-of select="TaskId"/>
        </td>
        <td class="field">
          <xsl:value-of select="CreatedDate"/>
        </td>
        <td class="field">
          <xsl:value-of select="ExecutionStart"/>
        </td>
        <td class="field">
          <xsl:value-of select="ExecutionEnd"/>
        </td>
      </tr>
      <tr >
        <td colspan="4" class="label">Summary</td>
      </tr>
      <tr>
        <td colspan="4" class="field">
          <xsl:value-of select="Title"/>
        </td>
      </tr>
      <tr>
        <td colspan="3" class="label">Description</td>
        <td colspan="1" class="label">Task variables</td>
      </tr>
      <tr height="150px">
        <td colspan="3" style="vertical-align:top" class="field">
          <div>
            <xsl:value-of select="Description"/>
          </div>
        </td>
        <td>
          <xsl:apply-templates select="NGinnTaskData" />
        </td>
      </tr>
    </table>
    </form>
  </xsl:template>

  <xsl:template match="NGinnTaskData">
    <table style="" width="100%">
      <xsl:for-each select="field">
        <tr>
          <td class="label">
            <b>
              <xsl:value-of select="@name"/>
            </b>
          </td>
        </tr>
        <tr>
          <td class="field2">
            <xsl:apply-templates select="." />
          </td>
        </tr>
      </xsl:for-each>
    </table>
  </xsl:template>

  <xsl:template match="field">
    <input type="text">
      <xsl:attribute name="name">
        <xsl:value-of select="@name"/>
      </xsl:attribute>
      <xsl:attribute name="value">
        <xsl:value-of select="@value"/>
      </xsl:attribute>
      <xsl:if test="@access != 'modify' and @access != 'required'">
        <xsl:attribute name="class">field_ro</xsl:attribute>
        <xsl:attribute name="readonly">1</xsl:attribute>
      </xsl:if>
      <xsl:if test="@access = 'modify'">
        <xsl:attribute name="class">field_rw</xsl:attribute>
      </xsl:if>
      <xsl:if test="@access = 'required'">
        <xsl:attribute name="class">field_rq</xsl:attribute>
      </xsl:if>
    </input>
  </xsl:template>

  <xsl:template match="field[option]">
    <select>
      <xsl:attribute name="name">
        <xsl:value-of select="@name"/>
      </xsl:attribute>
      <xsl:attribute name="value">
        <xsl:value-of select="@value"/>
      </xsl:attribute>
      <xsl:if test="@access != 'modify' and @access != 'required'">
        <xsl:attribute name="class">field_ro</xsl:attribute>
        <xsl:attribute name="readonly">1</xsl:attribute>
      </xsl:if>
      <xsl:if test="@access = 'modify'">
        <xsl:attribute name="class">field_rw</xsl:attribute>
      </xsl:if>
      <xsl:if test="@access = 'required'">
        <xsl:attribute name="class">field_rq</xsl:attribute>
      </xsl:if>
      <xsl:for-each select="option">
        <option><xsl:value-of select="."/></option>
      </xsl:for-each>
    </select>
  </xsl:template>
</xsl:stylesheet>
