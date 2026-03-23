// agent.show.template

const template: Template = {
	options: {
		noDirty: true
	},
	properties: {
		'TRoot.$$Tab': String,
		'TAgent.$SourceTransactions'() { return `/finance/transaction/partial/${this.Id}?Source=Agent`; },
		'TAgent.$SourceContracts'() { return `/catalog/contract/partial/${this.Id}?Source=Agent`; },
	/*	'TAgent.$SourceContacts'() { return `/catalog/agentcontact/partial/${this.Id}?Source=Agent`; },*/
		'TAgent.$SourceReports'() { return `/reports/partial/index/${this.Id}?Source=Agent`; },
		'TAgent.$SourceAttachments'() { return `/attachment/partial/${this.Id}?Source=Agent` }
	},
	commands: {
	},
	events: {
		'g.agent.saved': agentSaved,
	}
};

export default template;

function agentSaved(root) {
	let newAg = root.Agent;
	if (newAg.Id == this.Agent.Id)
		this.Agent.$merge(newAg);
}
