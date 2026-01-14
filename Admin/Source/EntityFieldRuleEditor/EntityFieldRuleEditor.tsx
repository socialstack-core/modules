import { CodeModuleType, CodeModuleTypeField, getEntities } from "Admin/Functions/GetPropTypes";
import ContentFieldAccessRuleApi, { ContentFieldAccessRule } from "Api/ContentFieldAccessRule";
import RoleApi, { Role } from "Api/Role";
import { useCallback, useEffect, useState } from "react";
import Alert from "UI/Alert";
import Button from "UI/Button";
import Form from "UI/Form";
import Input from "UI/Input";
import Loading from "UI/Loading";
import Modal from "UI/Modal";

/**
 * Props for the EntityFieldRuleEditor component
 */
export type EntityFieldRuleEditorProps = {
  entity: string | CodeModuleType;
  role: Role;
};


/**
 * Editor for managing field-level access rules for an entity and role
 */
const EntityFieldRuleEditor: React.FC<EntityFieldRuleEditorProps> = ({ entity: initialEntity, role }) => {
  const [entity, setEntity] = useState<CodeModuleType>();
  const [error, setError] = useState<string>();
  const [entityRules, setEntityRules] = useState<ContentFieldAccessRule[]>();
  const [currentEditField, setCurrentEditField] = useState<CodeModuleTypeField>();
  const [isCustomReadRule, setIsCustomReadRule] = useState<boolean>(false);
  const [isCustomWriteRule, setIsCustomWriteRule] = useState<boolean>(false);

  
  const fetchRules = useCallback(() => {
    
    if (!role) {
      return;
    }
    ContentFieldAccessRuleApi.list({
      query: "entityName = ? AND roleId = ?",
      args: [entity?.instanceName!, role.id],
    }).then((result) => setEntityRules(result.results));
  }, [entity, role]);

  // Load entity from string if needed
  useEffect(() => {
    if (!entity && !error) {
      if (typeof initialEntity === "string") {
        getEntities().then((allEntities) => {
          const found = allEntities.find((e) => e.instanceName === initialEntity);
          if (!found) {
            setError(`Cannot find the entity ${initialEntity}`);
          } else {
            setEntity(found);
          }
        });
      } else {
        setEntity(initialEntity);
      }
    }
  }, [entity, initialEntity, error]);

  const currentRule = entityRules?.find((rule) => rule.fieldName === currentEditField?.name);

  // Load access rules when entity or role changes
  useEffect(() => {
    if (entity) {
      fetchRules();
    }
  }, [entity, role, fetchRules]);

  // Reload rules when edit modal closes
  useEffect(() => {
    if (!currentEditField && entity) {
      fetchRules();
    }
    if (currentEditField) {
        setIsCustomReadRule(["", "false", "true"].includes(currentRule?.canRead ?? '') ? false : true);
        setIsCustomWriteRule(["", "false", "true"].includes(currentRule?.canRead ?? '') ? false : true);
    }
  }, [currentEditField, entity, fetchRules, currentRule]);


  if (!entity) {
    return error ? <Alert variant="danger">{error}</Alert> : <Loading />;
  }


  /**
   * Renders an access rule value as an icon
   */
  const renderValue = (value: string | null | undefined) => {
    if (value === "true") return <i className="fa fa-check" style={{ color: "green" }} />;
    if (value === "false") return <i className="fa fa-minus-circle" style={{ color: "red" }} />;
    if (value !== "") return <><i className="fa fa-check" style={{ color: "orange" }} /> {value}</>;
    return <i className="fa fa-check" style={{ color: "orange" }} />;
  };

  return (
    <div className="entity-field-rule-editor">
      {currentEditField && (
        <Modal
          visible={true}
          title={`Edit permissions for field '${currentEditField.name}'`}
          onClose={() => setCurrentEditField(undefined)}
        >
          <Alert variant="info">{`This only affects the role ${role.name}`}</Alert>
          <Form
            onSuccess={() => setCurrentEditField(undefined)}
            action={(values: ContentFieldAccessRule) => {
              const baseRule: ContentFieldAccessRule = {
                entityName: entity.instanceName,
                isVirtualType: false,
                fieldName: currentEditField.name,
                canRead: values.canRead === "" ? null : values.canRead,
                canWrite: values.canWrite === "" ? null : values.canWrite,
                roleId: role.id,
              };

              return currentRule?.id
                ? ContentFieldAccessRuleApi.update(currentRule.id, { ...baseRule, id: currentRule.id })
                : ContentFieldAccessRuleApi.create(baseRule);
            }}
          >
            <Input
              type="select"
              name={isCustomReadRule ? "_" : "canRead"}
              onChange={(ev) => {
                const value = (ev.target as HTMLSelectElement).value;
                if(value === "custom") {
                    setIsCustomReadRule(true);
                } else {
                    setIsCustomReadRule(false);
                    currentRule!.canRead = value;
                }
              }}
              label="Can read"
              defaultValue={["", "false", "true"].includes(currentRule?.canRead ?? '') ? currentRule?.canRead : "custom"}
            >
              <option value="">Inherited</option>
              <option value="false">Always denied</option>
              <option value="true">Always granted</option>
              <option value="custom">Custom rule</option>
            </Input>
            {isCustomReadRule && (
                <Input
                    type='text'
                    name='canRead'
                    label={`Enter custom rule`}
                    defaultValue={currentRule?.canRead}
                />
            )}
            <Input
              type="select"
              name={isCustomWriteRule ? "_" : "canWrite"}
              onChange={(ev) => {
                const value = (ev.target as HTMLSelectElement).value;
                if(value === "custom") {
                    setIsCustomWriteRule(true);
                } else {
                    setIsCustomWriteRule(false);
                    currentRule!.canWrite = value;
                }
              }}
              label="Can write"
              defaultValue={["", "false", "true"].includes(currentRule?.canWrite ?? '') ? currentRule?.canWrite : "custom"}
            >
              <option value="">Inherited</option>
              <option value="false">Always denied</option>
              <option value="true">Always granted</option>
              <option value="custom">Custom rule</option>
            </Input>
            {isCustomWriteRule && (
                <Input
                    type='text'
                    name='canWrite'
                    label={`Enter custom rule`}
                    defaultValue={currentRule?.canWrite}
                />
            )}

            <Button buttonType="submit">Save rules</Button>
          </Form>
        </Modal>
      )}

      <h4>{`${entity.instanceName} field rules`}</h4>
      <table className="table table-striped">
        <thead>
          <tr>
            <th>Field name</th>
            <th>Can read</th>
            <th>Can write</th>
          </tr>
        </thead>
        <tbody>
          {entity.fields?.map((field) => {
            const rule = entityRules?.find((r) => r.fieldName === field.name);
            return (
              <tr key={field.name}>
                <td>{field.name}</td>
                <td onClick={() => setCurrentEditField(field)}>{renderValue(rule?.canRead)}</td>
                <td onClick={() => setCurrentEditField(field)}>{renderValue(rule?.canWrite)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};

export default EntityFieldRuleEditor;
