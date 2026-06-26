<template>
	<div class="sys-user-container">
		<splitpanes class="default-theme">
			<pane size="20">
				<OrgTree ref="orgTreeRef" @node-click="nodeClick" />
			</pane>
			<pane size="80" style="overflow: auto;">
				<!-- 高级查询组件 -->
				<AdvancedSearch
					ref="searchRef"
					:fields="searchFields"
					:keywordFields="keywordFields"
					mode="sysUser"
					@query="handleAdvancedQuery"
					@reset="handleAdvancedReset"
				/>

				<el-card class="full-table" shadow="hover" style="margin-top: 5px">
					<template #header>
						<div style="display: flex; justify-content: flex-end;">
							<el-button type="primary" icon="ele-Plus" @click="openAddUser" v-auth="'sysUser:add'"> 新增 </el-button>
						</div>
					</template>
					<el-table :data="state.userData" style="width: 100%" v-loading="state.loading" border>
						<el-table-column type="index" label="序号" width="55" align="center" fixed />
						<el-table-column label="头像" width="80" align="center" show-overflow-tooltip>
							<template #default="scope">
								<el-avatar :src="scope.row.avatar" size="small">{{ scope.row.nickName?.slice(0, 1) ?? scope.row.realName?.slice(0, 1) }} </el-avatar>
							</template>
						</el-table-column>
						<el-table-column prop="account" label="账号" width="120" align="center" show-overflow-tooltip />
						<el-table-column prop="realName" label="姓名" width="120" align="center" show-overflow-tooltip />
						<el-table-column prop="phone" label="手机号码" width="120" align="center" show-overflow-tooltip />
						<el-table-column label="账号类型" width="110" align="center" show-overflow-tooltip>
							<template #default="scope">
								<g-sys-dict v-model="scope.row.accountType" code="AccountTypeEnum" />
							</template>
						</el-table-column>
						<el-table-column prop="roleName" label="角色集合" min-width="150" align="center" show-overflow-tooltip />
						<el-table-column prop="orgName" label="所属机构" min-width="120" align="center" show-overflow-tooltip />
						<el-table-column prop="posName" label="职位名称" min-width="120" align="center" show-overflow-tooltip />
						<el-table-column label="状态" width="70" align="center" show-overflow-tooltip>
							<template #default="scope">
								<el-switch v-model="scope.row.status" :active-value="1" :inactive-value="2" size="small" @change="changeStatus(scope.row)" v-auth="'sysUser:setStatus'" />
							</template>
						</el-table-column>
						<el-table-column prop="orderNo" label="排序" width="70" align="center" show-overflow-tooltip />
						<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
							<template #default="scope">
								<ModifyRecord :data="scope.row" />
							</template>
						</el-table-column>
						<el-table-column label="操作" width="300" align="center" fixed="right" show-overflow-tooltip>
							<template #default="scope">
								<el-tooltip content="编辑" placement="top">
									<el-button icon="ele-Edit" text type="primary" v-auth="'sysUser:update'" @click="openEditUser(scope.row)"> </el-button>
								</el-tooltip>
								<el-tooltip content="删除" placement="top">
									<el-button icon="ele-Delete" text type="danger" v-auth="'sysUser:delete'" @click="delUser(scope.row)"> </el-button>
								</el-tooltip>
								<el-tooltip content="复制" placement="top">
									<el-button icon="ele-CopyDocument" text type="primary" v-auth="'sysUser:add'" @click="openCopyMenu(scope.row)"> </el-button>
								</el-tooltip>
								<el-button icon="ele-RefreshLeft" text type="danger" v-auth="'sysUser:resetPwd'" @click="resetUserPwd(scope.row)">重置密码</el-button>
								<el-button icon="ele-Unlock" text type="primary" v-auth="'sysUser:unlockLogin'" @click="unlockLogin(scope.row)">解除锁定</el-button>
							</template>
						</el-table-column>
					</el-table>
					<el-pagination
						v-model:currentPage="state.tableParams.page"
						v-model:page-size="state.tableParams.pageSize"
						:total="state.tableParams.total"
						:page-sizes="[10, 20, 50, 100]"
						size="small"
						background
						@size-change="handleSizeChange"
						@current-change="handleCurrentChange"
						layout="total, sizes, prev, pager, next, jumper"
					/>
				</el-card>
			</pane>
		</splitpanes>

		<EditUser ref="editUserRef" :title="state.editUserTitle" :orgData="state.orgTreeData" @handleQuery="handleQuery" />
	</div>
</template>

<script lang="ts" setup name="sysUser">
import { onMounted, reactive, ref } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import OrgTree from '/@/views/system/org/component/orgTree.vue';
import EditUser from '/@/views/system/user/component/editUser.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { Splitpanes, Pane } from 'splitpanes';
import 'splitpanes/dist/splitpanes.css';
import { getAPI } from '/@/utils/axios-utils';
import { SysUserApi, SysOrgApi } from '/@/api-services/api';
import { SysUser, SysOrg, UpdateUserInput } from '/@/api-services/models';

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '账号', prop: 'Account', type: 'string' },
	{ label: '姓名', prop: 'realName', type: 'string' },
	{ label: '手机号码', prop: 'phone', type: 'string' },
	{ label: '职位名称', prop: 'posName', type: 'string' },
	{ label: '账号类型', prop: 'accountType', type: 'dicRange', dicCode: 'AccountTypeEnum' },
	{ label: '状态', prop: 'status', type: 'select', options: [
		{ label: '启用', value: 1 },
		{ label: '禁用', value: 2 },
	]},
];

// 关键字搜索字段列表（这些字段会进行 OR 模糊匹配）
const keywordFields = ['Account', 'realName', 'phone'];

const orgTreeRef = ref<InstanceType<typeof OrgTree>>();
const editUserRef = ref<InstanceType<typeof EditUser>>();
const searchRef = ref();
const state = reactive({
	loading: false,
	tenantList: [] as Array<any>,
	userData: [] as Array<SysUser>,
	orgTreeData: [] as Array<SysOrg>,
	queryParams: {
		orgId: -1,
		account: undefined,
		realName: undefined,
		phone: undefined,
		posName: undefined
	},
	advancedConditions: [] as QueryCondition[],
	tenantId: undefined,
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editUserTitle: '',
});

onMounted(async () => {
	await loadOrgData();
	await handleQuery();
});

// 查询机构数据
const loadOrgData = async () => {
	state.loading = true;
	let res = await getAPI(SysOrgApi).apiSysOrgListGet(0);
	state.orgTreeData = res.data.result ?? [];
	state.loading = false;
};

// 查询操作（原有逻辑）
const handleQuery = async () => {
	state.loading = true;
	let params = Object.assign(state.queryParams, state.tableParams);
	let res = await getAPI(SysUserApi).apiSysUserPagePost(params);
	state.userData = res.data.result?.items ?? [];
	state.tableParams.total = res.data.result?.total;
	state.loading = false;
};

// 高级查询
const handleAdvancedQuery = async (conditions: QueryCondition[]) => {
	console.log('handleAdvancedQuery 被调用, conditions:', conditions);
	state.advancedConditions = conditions;
	state.loading = true;

	// 获取关键字值
	const keywordValue = searchRef.value?.getKeyword?.() || '';

	// 构建通用 PageAdvancedInput 参数
	let params = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysUserApi).apiSysUserPageAdvancedPost(params as any);
		console.log('查询结果:', res.data);
		state.userData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	console.log('handleAdvancedReset 被调用');
	state.advancedConditions = [];
	await handleQuery();
};
// 点击机构树
const nodeClick = async (data: any) => {
	state.queryParams.orgId = data.id;
	await handleQuery();
};

// 打开新增页面
const openAddUser = () => {
	state.editUserTitle = '添加账号';
	editUserRef.value?.openDialog({ id: undefined, birthday: '2000-01-01', sex: 1, tenantId: state.tenantId, orderNo: 100, cardType: 0, cultureLevel: 5 });
};

// 打开编辑页面
const openEditUser = (row: any) => {
	state.editUserTitle = '编辑账号';
	editUserRef.value?.openDialog(row);
};

// 打开复制页面
const openCopyMenu = (row: any) => {
	state.editUserTitle = '复制账号';
	var copyRow = JSON.parse(JSON.stringify(row)) as UpdateUserInput;
	copyRow.id = 0;
	copyRow.account = '';
	editUserRef.value?.openDialog(copyRow);
};

// 删除
const delUser = (row: any) => {
	ElMessageBox.confirm(`确定要删除用户「${row.realName}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		await getAPI(SysUserApi).apiSysUserDeletePost({ id: row.id });
		await handleQuery();
		ElMessage.success('删除成功');
	}).catch(() => {});
};

// 重置密码
const resetUserPwd = (row: any) => {
	ElMessageBox.confirm(`确定要重置「${row.realName}」的密码吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		await getAPI(SysUserApi).apiSysUserResetPwdPost({ id: row.id });
		ElMessage.success('重置密码成功');
	}).catch(() => {});
};

// 解除锁定
const unlockLogin = (row: any) => {
	ElMessageBox.confirm(`确定要解除「${row.realName}」的锁定吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		await getAPI(SysUserApi).apiSysUserUnlockLoginPost({ id: row.id });
		ElMessage.success('解除锁定成功');
	}).catch(() => {});
};

// 修改状态
const changeStatus = async (row: any) => {
	await getAPI(SysUserApi).apiSysUserSetStatusPost({ id: row.id, status: row.status });
	ElMessage.success('修改状态成功');
};

// 改变页面容量
const handleSizeChange = async (val: number) => {
	state.tableParams.pageSize = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleQuery();
	}
};

// 改变页码序号
const handleCurrentChange = async (val: number) => {
	state.tableParams.page = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleQuery();
	}
};
</script>

<style scoped lang="scss">
.sys-user-container {
	height: 100%;
	display: flex;
	flex-direction: row !important;
}

:deep(.splitpanes) {
	height: 100%;
}

:deep(.splitpanes__pane) {
	overflow: hidden;
	display: flex;
	flex-direction: column;
}
</style>
