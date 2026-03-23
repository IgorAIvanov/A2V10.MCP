// agent/editfolder.template

const template: Template = {
	options: {
		globalSaveEvent: 'g.agentfolder.saved'
	},
	properties: {
		'TAgent.$Id'() { return this.Id || '@[NewItem]' },
	},
	validators: {
		'Agent.Name': '@[Error.Required]'
	},
	defaults: {
		"Agent.Folder"(this: any) { return this.Folders.length > 0 ? this.Folders[0] : undefined; }
	}
};

export default template;
