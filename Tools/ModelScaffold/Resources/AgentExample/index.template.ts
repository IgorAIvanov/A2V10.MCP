// agent.index

const folderutils = require('app:folders');

const template: Template = {
	options: {
		noDirty: true,
		persistSelect: ['Folders']
	},
	properties: {
		'TRoot.$CreateArg'() { return this.Folders.$selected ? { Folder: this.Folders.$selected.Id } : null },
		'TRoot.$EditFolderDisabled'() { return !this.Folders.$selected || this.Folders.$selected.Id < 0; },
		'TRoot.$EditFolderUrl'() { return '/catalog/agent/editfolder'; },
		'TRoot.$hasCheckedAgents'() { return this.Folders.$selected ? this.Folders.$selected.Agents.$hasChecked : false; },
		'TIntegration.$MenuText'() { return `Імпорт з ${this.Name}`; }
	},
	commands: {
		deleteFolder: folderutils.deleteFolderCommand('Agents'),
		moveToGroup: folderutils.moveToGroupCommand('Agents', '/catalog/agent')
	},
	events: {
		'g.tags.saved': tagsSaved,
		'g.agent.saved': agentSaved,
		'g.agent.reload'() { this.$ctrl.$reload(); },
		'g.agentfolder.saved'(root) { folderutils.handleSavedFolder.call(this, root.Agent); }
	}
};

export default template;

function invokeSignalR() {
	let ctrl: IController = this.$ctrl;
	ctrl.$invoke("signalR", { userId: 99, message: "notify.reload", data: { test: 22, expr: '7+3' } });
}

function allAgents(root) {
	let res = [];
	let traverse = x => {
		x.SubItems.forEach(s => traverse(s));
		x.Agents.forEach(a => res.push(a));
	}
	root.forEach(f => traverse(f));
	return res;
}

function agentSaved(root) {
	let newAg = root.Agent;
	let arr = this.Folders.$selected.Agents;
	let oldAg = arr.$find(w => w.Id === newAg.Id);
	if (oldAg)
		oldAg.$merge(newAg);
	else
		arr.$append(newAg);
}

function tagsSaved(root) {
	if (root.Params.For !== 'Agent') return;

	let tags = root.Tags;
	this.Tags.$copy(tags);

	let ag = allAgents(this.Folders);
	ag.forEach(ag => {
		ag.Tags.forEach(at => {
			let nt = tags.find(tg => tg.Id == at.Id);
			if (nt) at.$merge(nt);
		});
	});
}


