/* Agent */
-------------------------------------------------
create or alter procedure cat.[Agent.Index]
@UserId bigint,
@Id bigint = null,
@StdFolders bit = 1
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	declare @canView bit, @canViewAll bit, @permissions int;

	exec appsec.GetUserPermissions N'Cat.Agent', @UserId,	
		@CanView = @canView output,@CanViewAll = @canViewAll output, @Permissions = @permissions output;


	with T(Id, [Name], Icon, HasChildren, [Order])
	as (
		select Id = cast(-1 as bigint), [Name] = N'[@[WithoutGrouping]]', Icon='users',
			HasChildren = cast(0 as bit), [Order] = 1
		where @StdFolders = 1
		union all
		select Id = cast(-2 as bigint), [Name] = N'[@[NotInGroups]]', Icon='folder-ban',
			HasChildren = cast(0 as bit), [Order] = 8
		where @StdFolders = 1
		union all
		select Id, [Name], Icon = N'folder-outline',
			HasChildren= case when exists(select 1 from cat.Agents c where  c.Void = 0 and c.Parent = a.Id and c.IsFolder = 1 and c.IsAgent = 1) then 1 else 0 end,
			[Order] = 2
		from cat.Agents a
		where  (@canViewAll = 1 or (@canView = 1 and a.UserCreated = @UserId))
		and a.IsFolder = 1 and a.IsAgent = 1 and a.Void = 0 and a.Parent is null
	)
	select [Folders!TFolder!Tree] = null, [Id!!Id] = Id, [Name!!Name] = [Name], Icon,
		[SubItems!TFolder!Items] = null, 
		[HasSubItems!!HasChildren] = HasChildren,
		[!!Permissions] = @permissions,
		[Agents!TAgent!LazyArray] = null
	from T
	order by [Order], [Name];

	select [Agents!TAgent!Array] = null, [Id!!Id] = a.Id, [Name!!Name] = [Name], NameEN, CodeUA, ActedName,
		[Memo], PersonId = a.Person, [Owner!TOwner!RefId] = a.[UserCreated],
		[BankAccounts!TAgBankAcc!Array] = null,
		[!!Permissions] = 0, [!!RowCount] = 0
	from cat.Agents a
	where 0 <> 0;

	select [!TAgBankAcc!Map] = null, [Id!!Id] = ba.Id, [Name] = ba.[Name], ba.IBAN
	from cat.AgentBankAccounts ba where 0 <> 0

	select [!TOwner!Map] = null, [Id!!Id] = u.Id,
		u.[UserName] as [Name], u.[Email]
	from a2security.Users u
	where 0<> 0; 

	select [!$System!] = null, [!Agents!Permissions] = @permissions, [!Folders!Permissions] = @permissions;

end
go
-------------------------------------------------
create or alter procedure cat.[Agent.Expand]
@UserId bigint,
@Id bigint = null,
@StdFolders bit = 1
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	select [SubItems!TFolder!Tree] = null, [Id!!Id] = Id, [Name!!Name] = [Name], Icon = N'folder-outline',
		[SubItems!TFolder!Items] = null,
		[HasSubItems!!HasChildren] = case when exists(select 1 from cat.Agents c where c.Void=0 and c.Parent=a.Id and c.IsFolder = 1 and a.IsAgent = 1) then 1 else 0 end,
		[Children!TAgent!LazyArray] = null
	from cat.Agents a where IsFolder=1 and Parent = @Id and Void=0 and a.IsAgent = 1;
end
go
-------------------------------------------------
create or alter procedure cat.[Agent.Agents]
@UserId bigint,
@Id bigint = null,
@PageSize int = 20,
@Offset int = 0,
@Order nvarchar(255) = N'name',
@Dir nvarchar(20) = N'asc',
@Fragment nvarchar(255) = null
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;
	
	declare @canView bit, @canViewAll bit, @permissions int;

	exec appsec.GetUserPermissions N'Cat.Agent', @UserId,	
		@CanView = @canView output,@CanViewAll = @canViewAll output, @Permissions = @permissions output;

	set @Order = lower(@Order);
	set @Dir = lower(@Dir);

	declare @fr nvarchar(255);
	set @fr = N'%' + @Fragment + N'%';

	declare @agents table(rowno int identity(1, 1), id bigint, rowcnt int);

	insert into @agents(id, rowcnt)
	select a.Id, count(*) over()
	from cat.Agents a
	where a.Void = 0 and IsAgent = 1 and a.IsFolder = 0 and (@Id = -1 or a.Parent = @Id or @Id = -2 and a.Parent is null)
		and (@canViewAll = 1 or (@canView = 1 and a.UserCreated = @UserId))
		and (@fr is null or a.[Name] like @fr or a.CodeUA like @fr or a.NameEN like @fr or a.Memo like @fr)
	order by 
		case when @Dir = N'asc' then
			case @Order 
				when N'id' then a.Id
				when N'person' then a.Person
			end
		end asc,
		case when @Dir = N'asc' then
			case @Order 
				when N'name' then a.[Name]
				when N'code' then a.[CodeUA]
				when N'memo' then a.[Memo]
				when N'actedname' then a.[ActedName]
			end
		end asc,
		-- desc
		case when @Dir = N'desc' then
			case @Order
				when N'id' then a.Id
				when N'person' then a.Person
			end
		end desc,
		case when @Dir = N'desc' then
			case @Order 
				when N'name' then a.[Name]
				when N'code' then a.[CodeUA]
				when N'memo' then a.[Memo]
				when N'actedname' then a.[ActedName]
			end
		end desc,
		a.Id 
	offset (@Offset) rows fetch next (@PageSize) rows only
	option (recompile);

	select [Agents!TAgent!Array] = null, [Id!!Id] = a.Id, [Name!!Name] = [Name], a.CodeUA, a.ActedName,
		NameEN, [Memo], PersonId = a.Person, [Owner!TOwner!RefId] = a.[UserCreated],
		[BankAccounts!TAgBankAcc!Array] = null,
		[!!Permissions] = @permissions, [!!RowCount]  = t.rowcnt
	from cat.Agents a
		inner join @agents t on a.Id = t.id;

	with T as (
		select distinct UserCreated
		from cat.Agents a
			inner join @agents t on a.Id = t.id
	)
	select [!TOwner!Map] = null, [Id!!Id] = u.Id,
	u.[UserName] as [Name], u.[Email]
	from a2security.Users u
	inner join T on u.Id = T.UserCreated;
		
	select [!TAgBankAcc!Array] = null, [Id!!Id] = ba.Id, [Name] = ba.[Name], ba.IBAN,
		[!TAgent.BankAccounts!ParentId] = ba.Agent
	from cat.AgentBankAccounts ba inner join @agents a on a.id = ba.Agent;

	-- system data
	select [!$System!] = null,
		[!Agents!PageSize] = @PageSize,  [!Agents!Offset] = @Offset,
		[!Agents!SortOrder] = @Order,  [!Agents!SortDir] = @Dir, [!Agents!Permissions] = @permissions,
		[!Agents.Fragment!Filter] = @Fragment;
end
go
-------------------------------------------------
create or alter procedure cat.[Agent.Load]
@UserId bigint,
@Id bigint = null,
@Folder bigint = null
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	--declare @readOnly bit;
	--declare @canCreate bit;

	--exec appsec.GetUserPermissions N'Cat.Agent', @UserId,	
	--	@IsReadOnly = @readOnly output, @CanCreate = @canCreate output;

	--if @readOnly = 1 and (
	--		@UserId = (select UserCreated from cat.Agents where Id = @Id) 
	--		or @canCreate = 1 and @Id is null)
	--	set @readOnly = 0;

	declare @readOnly bit = 0, @permissions int;
	declare @UserCreated bigint ;
	set @UserCreated = @UserId;

	if @Id is not null
		select @UserCreated = isNull(UserCreated, 99) from cat.Agents where Id = @Id;
	exec appsec.[CheckUserPermissions] N'Cat.Agent', @UserId, @Id, @UserCreated, @ReadOnly = @readOnly output;

	exec appsec.GetUserPermissions N'Cat.Agent', @UserId, @Permissions = @permissions output;

	if @Folder is null and @Id is not null
		select @Folder = Parent from cat.Agents where Id = @Id;

	select [Agent!TAgent!Object] = null, [Id!!Id] = a.Id, [Name!!Name] = [Name],
		NameEN, CodeUA, [Memo], ActedName, PersonId = a.Person,
		 [Owner!TOwner!RefId] = a.[UserCreated],
		[Folder!TFolder!RefId] = a.Parent,
		[Contacts!TContacts!Array] = null,
		[BankAccounts!TBankAccounts!Array] = null,
		[Contracts!TContracts!Array] = null,
		[Names!TName!Array] = null
	from cat.Agents a
	where Id = @Id;


	select [Folders!TFolder!Map] = null, [Id!!Id] = a.Id, [Name!!Name] = a.[Name]
	from cat.Agents a where a.Id = @Folder;

	select [!TOwner!Map] = null, [Id!!Id] = u.Id,
	u.[UserName] as [Name], u.[Email]
	from a2security.Users u
	where u.Id = @UserCreated;

	--agent contacts
	select [!TContacts!Array] = null, [Id!!Id] = c.Id, 
	[!TAgent.Contacts!ParentId]=c.Agent,
	c.[Name], c.[NameEN], c.Position, c.Email, c.Phone, c.[Address], c.AdressEN, c.FactAddress,	c.FactAddressEN
	from cat.AgentContacts c
	where c.Agent = @Id and c.Void = 0; 


	select [!TBankAccounts!Array] = null, [Id!!Id] = ba.Id, 
	[!TAgent.BankAccounts!ParentId] = ba.Agent,
	[Currency!TCurrency!RefId] = ba.Currency, [Bank!TBank!RefId] =  ba.Bank,
	ba.[Name], ba.NameEN, 
	ba.Memo, ba.IBAN
	from cat.AgentBankAccounts ba
	where ba.Agent = @Id and ba.Void = 0;

	select [!TContracts!Array] = null, [Id!!Id] = c.Id, 
	[!TAgent.Contracts!ParentId] = c.Agent,
	c.[Name], c.[NameEN], c.[Memo], c.[SNo], c.[Date]
	from cat.Contracts c
	where c.Agent = @Id and c.Void = 0;

	with T as(
		select Currency
		from cat.AgentBankAccounts where Agent = @Id and Void = 0
	)
	select [!TCurrency!Map] = null, [Id!!Id] = c.Id, [Name!!Name] = c.[Name], c.Alpha3
	from cat.Currencies c
	inner join T on c.Id = T.Currency;

	with T as(
		select Bank
		from cat.AgentBankAccounts where Agent = @Id and Void = 0
	)
	select [!TBank!Map] = null, [Id!!Id] = b.Id, [Name!!Name] = b.[Name], b.[Code]
	from cat.Banks b
	inner join T on b.Id = T.Bank;


	select [!TName!Array] = null, [Id!!Id] = Id, [Name],
		[!TAgent.Names!ParentId] = Agent
	from cat.AgentNames where Agent = @Id;

	select [!$System!] = null, [!!ReadOnly] = @readOnly;
end
go
-------------------------------------------------
create or alter procedure cat.[Agent.Folder.Load]
@UserId bigint,
@Id bigint = null,
@Folder bigint = null
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	if @Folder is null and @Id is not null
		select @Folder = Parent from cat.Agents where Id = @Id;

	select [Agent!TAgent!Object] = null, [Id!!Id] = a.Id, [Name!!Name] = [Name], [Memo],
		[Folder!TFolder!RefId] = a.Parent
	from cat.Agents a
	where Id = @Id and IsFolder = 1;

	select [Folders!TFolder!Map] = null, [Id!!Id] = a.Id, [Name!!Name] = a.[Name]
	from cat.Agents a where a.Id = @Folder;
end
go
-------------------------------------------------
drop procedure if exists cat.[Agent.Metadata];
drop procedure if exists cat.[Agent.Update];
drop procedure if exists cat.[Agent.Folder.Metadata];
drop procedure if exists cat.[Agent.Folder.Update];
drop type if exists cat.[Agent.TableType];
go
-------------------------------------------------
create type cat.[Agent.TableType]
as table(
	Id bigint null,
	[Name] nvarchar(255),
	[NameEN] nvarchar(255),
	[CodeUA] nvarchar(16),
	[Memo] nvarchar(255),
	[ActedName] nvarchar(255),
	Folder bigint
)
go
------------------------------------------------
create or alter procedure cat.[Agent.Metadata]
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;
	declare @Agent cat.[Agent.TableType];
	declare @Tag a2sys.[Id.TableType];
	select [Agent!Agent!Metadata] = null, * from @Agent;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.Folder.Metadata]
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;
	declare @Agent cat.[Agent.TableType];
	select [Agent!Agent!Metadata] = null, * from @Agent;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.Update]
@UserId bigint,
@Agent cat.[Agent.TableType] readonly
as
begin
	set nocount on;
	set transaction isolation level read committed;

	/*
	declare @xml nvarchar(max);
	set @xml = (select * from @Tags for xml auto);
	throw 60000, @xml, 0;
	*/

	declare @rtable table(id bigint);
	declare @id bigint;

	merge cat.Agents as t
	using @Agent as s
	on t.Id = s.Id
	when matched then update set
		t.[Name] = s.[Name],
		t.[Memo] = s.[Memo],
		t.[NameEN] = s.[NameEN],
		t.CodeUA = s.CodeUA,
		t.ActedName = s.ActedName,
		t.[IsAgent] = 1,
		t.UserCreated = isnull(t.UserCreated, @UserId)
	when not matched by target then insert
		([Name], NameEN, CodeUA, Memo, IsAgent, ActedName, [Parent], UserCreated) values
		(s.[Name], s.NameEN, s.CodeUA, s.Memo, 1, s.ActedName, s.Folder, @UserId)
	output inserted.Id into @rtable(id);
	select top(1) @id = id from @rtable;

	exec cat.[Agent.Load] @UserId = @UserId, @Id = @id;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.Folder.Update]
@UserId bigint,
@Agent cat.[Agent.TableType] readonly
as
begin
	set nocount on;
	set transaction isolation level read committed;

	declare @rtable table(id bigint);
	declare @id bigint;

	merge cat.Agents as t
	using @Agent as s
	on t.Id = s.Id
	when matched then update set
		t.[Name] = s.[Name],
		t.[Memo] = s.[Memo]
	when not matched by target then insert
		([Name], Memo, Parent, IsFolder, IsAgent) values
		(s.[Name], s.Memo, s.Folder, 1, 1)
	output inserted.Id into @rtable(id);
	select top(1) @id = id from @rtable;
	exec cat.[Agent.Folder.Load] @UserId = @UserId, @Id = @id;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.Fetch]
@UserId bigint,
@Text nvarchar(255)
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	declare @fr nvarchar(255);
	set @fr = N'%' + @Text + N'%';
	
	declare @agents table(Id bigint);
	insert into @agents (Id)
	select top(100) a.Id
	from cat.Agents a
	where Void = 0 and a.IsAgent = 1 and a.IsFolder = 0 and
		([Name] like @fr or Memo like @fr or NameEN like @fr or CodeUA like @fr)
	order by a.[Name];


	select top(100) [Agents!TAgent!Array] = null, [Id!!Id] = a.Id, [Name!!Name] = a.[Name], a.Memo, 
		a.NameEN, a.CodeUA, a.ActedName,
		[BankAccounts!TAgBanAcc!Array] = null
	from cat.Agents a inner join @agents t on a.Id = t.Id
	order by a.[Name];

	select [!TAgBankAcc!Array] = null, [Id!!Id] = ba.Id, [Name] = ba.[Name], ba.IBAN,
		[!TAgent.BankAccounts!ParentId] = ba.Agent
	from cat.AgentBankAccounts ba inner join @agents a on a.id = ba.Agent;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.Folder.Delete]
@UserId bigint,
@Id bigint
as
begin
	set nocount on;
	set transaction isolation level read committed;

	if exists(select * from cat.Agents where Parent = @Id and Void = 0)
		throw 60000, N'UI:@[Error.Delete.Folder]', 0;

	update cat.Agents set Void = 1 where Id = @Id and IsFolder = 1 and IsAgent = 1;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.Agents.Delete]
@UserId bigint,
@Id bigint
as
begin
	set nocount on;
	set transaction isolation level read committed;

	declare @canDelete bit = 0, @canDeleteAll bit = 0, @permissions int;
	declare @UserCreated bigint;

	exec appsec.GetUserPermissions N'Cat.Agent', @UserId,	
		@CanDelete = @canDelete output, @CanDeleteAll = @canDeleteAll output, @Permissions = @permissions output;
	select @UserCreated = isnull(UserCreated, 99) from cat.Agents where Id = @Id;

	if (@canDeleteAll = 0 and @canDelete = 0) or (@canDelete=1 and  @canDeleteAll = 0 and @UserCreated <> @UserId)
				throw 60000, N'UI:@[Error.NoPermissions.Delete]', 0; 

	-- TODO: check other modules
	if exists(select * from cat.Agents where Parent = @Id and Void = 0) or
	   exists(select * from doc.Transactions where Agent = @Id) 
			throw 60000, N'UI:@[Error.Delete.Used]', 0;

	update cat.Agents set Void = 1 where Id = @Id and IsFolder = 0;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.MoveToFolder]
@UserId bigint,
@Folder bigint,
@Items nvarchar(1024)
as
begin
	set nocount on;
	set transaction isolation level read committed;

	update cat.Agents set Parent = @Folder
	from cat.Agents a
		inner join string_split(@Items, N',') t
	on a.Id = cast(t.[value] as bigint);
end
go
-------------------------------------------------
create or alter procedure cat.[Agent.Show.Load]
@UserId bigint,
@Id bigint = null,
@Folder bigint = null,
@From date = null,
@To date = null
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	exec cat.[Agent.Load] @UserId = @UserId, @Id = @Id, @Folder = @Folder;

	set @From = isnull(@From, getdate());
	set @To = isnull(@To, @From);

	select [Params!TParam!Object] = null, [Period.From!TPeriod!] = @From, [Period.To!TPeriod!] = @To;
end
go
-------------------------------------------------
create or alter procedure cat.[Agent.SetNameEn.Load]
@UserId bigint
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	select [Agents!TAgent!Array] = null, [Id!!Id] = Id, [Name], NameEN
	from cat.Agents where Void = 0 and NameEN is null;
end
go
------------------------------------------------
drop procedure if exists cat.[Agent.SetNameEn.Metadata];
drop procedure if exists cat.[Agent.SetNameEn.Update];
drop type if exists cat.[Agent.SetNameEn.TableType];
go
------------------------------------------------
create type cat.[Agent.SetNameEn.TableType] as table
(
	Id bigint,
	NameEN nvarchar(255)
)
go
------------------------------------------------
create or alter procedure cat.[Agent.SetNameEn.Metadata]
as
begin
	set nocount on;

	declare @Agents cat.[Agent.SetNameEn.TableType];
	select [Agents!Agents!Metadata] = null, * from @Agents;
end
go
------------------------------------------------
create or alter procedure cat.[Agent.SetNameEn.Update]
@UserId bigint,
@Agents cat.[Agent.SetNameEn.TableType] readonly
as
begin
	set nocount on;
	set transaction isolation level read committed;

	/*
	declare @xml nvarchar(max);
	set @xml = (select * from @Agents for xml auto);
	throw 60000, @xml, 0;
	*/
	update cat.Agents set NameEN = s.NameEN
	from cat.Agents a inner join @Agents s on a.Id = s.Id
	and a.NameEN is null;

end
go
------------------------------------------------
create or alter procedure cat.[Agent.SetAsEmployee]
@UserId bigint,
@Id bigint
as
begin
	set nocount on;
	set transaction isolation level read committed;

	declare @personId bigint, @taxno nvarchar(255);

	select @personId = Id from cat.Persons where Agent = @Id;
	if @personId is not null
		throw 60000, N'The agent is already an employee', 0;

	select @taxno = CodeUA from cat.Agents where Id = @Id;
	select @personId = Id from 
		cat.Persons where TaxNo = @taxno;

	if @personId is not null
		update cat.Persons set Agent = @Id where Id = @personId;
	else
	begin
		declare @rtable table(id bigint);
		
		insert into cat.Persons([Name], FamilyNameEN, TaxNo, Agent)
		output inserted.Id into @rtable(id)
		select [Name], NameEN, CodeUA, Id
		from cat.Agents a where Id = @Id;

		select @personId = id from @rtable;

		update cat.Agents set Person = @personId where Id = @Id;
	end

	select [Result!TResult!Object] = null, Id = @personId;
end
go

------------------------------------------------
create or alter procedure cat.[Agent.Code.CheckDuplicate]
@UserId bigint,
@Id bigint = 0,
@Code nvarchar(16) = null,
@Folder bigint = null -- filter
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	declare @valid bit = 1;

	if exists(select 1 from cat.Agents where CodeUA = @Code and Id <> @Id)
		set @valid = 0;

	select [Result!TResult!Object] = null, [Value] = @valid;
end
go

------------------------------------------------
create or alter procedure cat.[Agent.FindByCode]
@UserId bigint,
@Id bigint = 0,
@Code nvarchar(16) = null,
@Folder bigint = null -- filter
as
begin
	set nocount on;
	set transaction isolation level read uncommitted;

	select top(1) [Agent!TAgent!Object] = null, [Id!!Id] = Id, [Name!!Name] = [Name], CodeUA
	from cat.Agents where CodeUA = @Code and Void = 0
	order by Id desc; -- LAST
end
go
