
const template: Template = {
	options: {
		globalSaveEvent: 'g.agent.saved'
	},
	properties: {
		'TRoot.$$Tab': String,
		'TAgent.$Title'() { return this.Id || '@[NewItem]' },
		'TAgent.$CanSetAsEmpl'() { return this.Id && !this.PersonId; }
	},
	validators: {
		'Agent.Name': '@[Error.Required]',
		'Agent.CodeUA': { valid: checkDuplicateCode, async: true, msg: '@[Agent.DuplicateCode]' }
	},
	commands: {
		setAsEmployee,
		newContact,
		newContract,
		newAgentBankAccount,
		makeBankAccountDelete,
		makeContactDelete,
		makeContractDelete
	},
	defaults: {
		"Agent.Folder"(this: any) { return this.Folders.length > 0 ? this.Folders[0] : undefined; }
	},
	events: {
		'Agent.Name.change': nameChange
	}
};

export default template;


async function setAsEmployee() {
	const ctrl: IController = this.$ctrl;
	let res = await ctrl.$invoke('setAsEmployee', { Id: this.Agent.Id });
	this.Agent.PersonId = res.Result.Id;
	ctrl.$emitGlobal('g.agent.saved', this);
	ctrl.$toast("Command completed successfully", CommonStyle.success);	
}

async function newContact() {
	const ctrl: IController = this.$ctrl;
	let res = await ctrl.$showDialog('/catalog/agentcontact/edit', null, { Agent: this.Agent.Id });
    this.Agent.Contacts.$prepend(res);
}

async function newContract() {
	const ctrl: IController = this.$ctrl;
	let res = await ctrl.$showDialog('/catalog/contract/edit', null, { Agent: this.Agent.Id });
	this.Agent.Contracts.$prepend(res);
}

async function newAgentBankAccount() {
	const ctrl: IController = this.$ctrl;
	let res = await ctrl.$showDialog('/catalog/agentbankaccount/edit', null, { Agent: this.Agent.Id });
	this.Agent.BankAccounts.$prepend(res);
}

async function makeBankAccountDelete(el) {
	const ctrl: IController = this.$ctrl;
	await ctrl.$invoke('deleteBankAccount', { Id:el.Id });
    this.Agent.BankAccounts.$remove(el);	
}

async function makeContactDelete(el) {
	const ctrl: IController = this.$ctrl;
	await ctrl.$invoke('deleteContact', { Id: el.Id });
	this.Agent.Contacts.$remove(el);
}

async function makeContractDelete(el) {
	const ctrl: IController = this.$ctrl;
	await ctrl.$invoke('deleteContract', { Id: el.Id });
	this.Agent.Contracts.$remove(el);
}


async function nameChange(ag) {
	const ctrl: IController = this.$ctrl;
	let res = await ctrl.$invoke('translit', { Source: ag.Name }, '/catalog/agent');
	ag.NameEN = res.Result.toUpperCase();
}

function checkDuplicateCode(a, code) {
	if (!code) return true;
	return a.$vm.$asyncValid('checkDuplicate', { Id: a.Id, Code: code });
}