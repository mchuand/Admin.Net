<template>
	<div>
		<el-card class="full-table sys-tenant-card" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysTenant" :buttonConfig="tenantButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysTenant" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table :data="state.tenantData" style="width: 100%" v-loading="state.loading" border>
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="logo" label="图标" width="55" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-avatar shape="square" :src="scope.row.logo" size="small" />
					</template>
				</el-table-column>
				<el-table-column prop="name" label="名称" width="180" align="center" show-overflow-tooltip />
				<el-table-column prop="title" label="标题" width="180" show-overflow-tooltip />
				<el-table-column prop="viceTitle" label="副标题" width="180" show-overflow-tooltip />
				<el-table-column prop="viceDesc" label="描述" width="300" show-overflow-tooltip />
				<el-table-column prop="watermark" label="水印" width="130" show-overflow-tooltip />
				<el-table-column prop="copyright" label="版权信息" width="350" show-overflow-tooltip />
				<el-table-column prop="icp" label="备案号" width="130" show-overflow-tooltip />
				<el-table-column prop="icpUrl" label="icp地址" width="280" show-overflow-tooltip />
				<el-table-column prop="enableReg" label="启用注册" width="280" show-overflow-tooltip>
					<template #default="scope">
						<g-sys-dict v-model="scope.row.enableReg" code="YesNoEnum" />
					</template>
				</el-table-column>
				<el-table-column prop="adminAccount" label="租管账号" align="center" width="120" show-overflow-tooltip />
				<el-table-column prop="phone" label="电话" width="120" align="center" show-overflow-tooltip />
				<el-table-column prop="host" label="域名" width="150" show-overflow-tooltip />
				<el-table-column prop="tenantType" label="租户类型" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<g-sys-dict v-model="scope.row.tenantType" code="TenantTypeEnum" />
					</template>
				</el-table-column>
				<el-table-column label="状态" width="70" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-switch v-model="scope.row.status" :active-value="1" :inactive-value="2" size="small"
							@change="changeStatus(scope.row)" :disabled="scope.row.id == 123456780000000" />
					</template>
				</el-table-column>
				<el-table-column prop="dbType" label="数据库类型" width="120" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-tag v-if="scope.row.dbType === 0"> MySql </el-tag>
						<el-tag v-else-if="scope.row.dbType === 1"> SqlServer </el-tag>
						<el-tag v-if="scope.row.dbType === 2"> Sqlite </el-tag>
						<el-tag v-else-if="scope.row.dbType === 3"> Oracle </el-tag>
						<el-tag v-if="scope.row.dbType === 4"> PostgreSQL </el-tag>
						<el-tag v-else-if="scope.row.dbType === 5"> Dm </el-tag>
						<el-tag v-if="scope.row.dbType === 6"> Kdbndp </el-tag>
						<el-tag v-else-if="scope.row.dbType === 7"> Oscar </el-tag>
						<el-tag v-if="scope.row.dbType === 8"> MySqlConnector </el-tag>
						<el-tag v-else-if="scope.row.dbType === 9"> Access </el-tag>
						<el-tag v-if="scope.row.dbType === 10"> OpenGauss </el-tag>
						<el-tag v-else-if="scope.row.dbType === 11"> QuestDB </el-tag>
						<el-tag v-if="scope.row.dbType === 12"> HG </el-tag>
						<el-tag v-else-if="scope.row.dbType === 13"> ClickHouse </el-tag>
						<el-tag v-else-if="scope.row.dbType === 14"> GBase </el-tag>
						<el-tag v-else-if="scope.row.dbType === 15"> Odbc </el-tag>
						<el-tag v-else-if="scope.row.dbType === 16"> OceanBaseForOracle </el-tag>
						<el-tag v-else-if="scope.row.dbType === 17"> TDengine </el-tag>
						<el-tag v-else-if="scope.row.dbType === 18"> GaussDB </el-tag>
						<el-tag v-else-if="scope.row.dbType === 19"> OceanBase </el-tag>
						<el-tag v-else-if="scope.row.dbType === 20"> Tidb </el-tag>
						<el-tag v-else-if="scope.row.dbType === 21"> Vastbase </el-tag>
						<el-tag v-else-if="scope.row.dbType === 22"> PolarDB </el-tag>
						<el-tag v-else-if="scope.row.dbType === 23"> Doris </el-tag>
						<el-tag v-else-if="scope.row.dbType === 900"> Custom </el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="connection" label="数据库连接" min-width="300" header-align="center"
					show-overflow-tooltip />
				<el-table-column prop="slaveConnections" label="从库连接" min-width="300" header-align="center"
					show-overflow-tooltip />
				<el-table-column prop="orderNo" label="排序" width="70" show-overflow-tooltip />
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
				<el-table-column label="操作" width="200" fixed="right" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-button icon="ele-Coin" size="small" text type="danger" @click="createTenant(scope.row)"
							v-auth="'sysTenant:createDb'" :disabled="scope.row.tenantType == 0"> 创建库 </el-button>
						<el-button icon="ele-Edit" size="small" text type="primary" @click="openEditTenant(scope.row)"
							v-auth="'sysTenant:update'"> 编辑 </el-button>
						<el-dropdown>
							<el-button icon="ele-MoreFilled" size="small" text type="primary"
								style="padding-left: 12px" />
							<template #dropdown>
								<el-dropdown-menu>
									<el-dropdown-item icon="ele-OfficeBuilding" @click="goTenant(scope.row)"
									                  :v-auth="'sysTenant:goTenant'"> 进入租管端 </el-dropdown-item>
									<el-dropdown-item icon="ele-OfficeBuilding" @click="changeTenant(scope.row)"
									                  :v-auth="'sysTenant:changeTenant'"> 切换租户 </el-dropdown-item>
									<el-dropdown-item icon="ele-OfficeBuilding" @click="openGrantMenu(scope.row)"
										:v-auth="'sysTenant:grantMenu'"> 授权菜单 </el-dropdown-item>
									<el-dropdown-item icon="ele-OfficeBuilding" @click="syncGrantMenu(scope.row)"
									                  :v-auth="'sysTenant:syncGrantMenu'" title="用于版本更新后，同步授权数据"> 同步授权 </el-dropdown-item>
									<el-dropdown-item icon="ele-RefreshLeft" @click="resetTenantPwd(scope.row)"
										:v-auth="'sysTenant:resetPwd'"> 重置密码 </el-dropdown-item>
									<el-dropdown-item icon="ele-Delete" @click="delTenant(scope.row)"
										:v-auth="'sysTenant:delete'"> 删除租户 </el-dropdown-item>
								</el-dropdown-menu>
							</template>
						</el-dropdown>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page" v-model:page-size="state.tableParams.pageSize"
				:total="state.tableParams.total" :page-sizes="[10, 20, 50, 100]" size="small" background
				@size-change="handleSizeChange" @current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper" />
		</el-card>

		<EditTenant ref="editTenantRef" :title="state.editTenantTitle" @handleQuery="handleAdvancedQuery([])" />
		<GrantMenu ref="grantMenuRef" />
	</div>
</template>

<script lang="ts" setup name="sysTenant">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import EditTenant from '/@/views/system/tenant/component/editTenant.vue';
import GrantMenu from '/@/views/system/tenant/component/grantMenu.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { getAPI } from '/@/utils/axios-utils';
import { SysTenantApi } from '/@/api-services/api';
import { TenantOutput } from '/@/api-services/models';
import { reLoadLoginAccessToken } from "/@/utils/request";
import GSysDict from "/@/components/sysDict/sysDict.vue";

const editTenantRef = ref<InstanceType<typeof EditTenant>>();
const grantMenuRef = ref<InstanceType<typeof GrantMenu>>();
const searchRef = ref();
const selectRows = shallowRef<any[]>([]);

const state = reactive({
	loading: false,
	tenantData: [] as Array<TenantOutput>,
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editTenantTitle: '',
});

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '租户名称', prop: 'name', type: 'string' },
	{ label: '联系电话', prop: 'phone', type: 'string' },
	{ label: '域名', prop: 'host', type: 'string' },
	{ label: '租管账号', prop: 'adminAccount', type: 'string' },
];

// 关键字搜索字段列表
const keywordFields = ['name', 'phone', 'host', 'adminAccount'];

// 按钮栏配置
const tenantButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增', icon: 'ele-Plus', color: 'primary' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddTenant(); break;
	}
};

onMounted(async () => {
	await handleAdvancedQuery([]);
});

// 高级查询（所有查询统一走此方法）
const handleAdvancedQuery = async (conditions: QueryCondition[]) => {
	state.advancedConditions = conditions;
	state.loading = true;

	const keywordValue = searchRef.value?.getKeyword?.() || '';

	let params = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysTenantApi).apiSysTenantPageAdvancedPost(params as any);
		state.tenantData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

// 进入租管端
const goTenant = (row: any) => {
	ElMessageBox.confirm(`确定要进入【${row.name}】租管端?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(() =>
		getAPI(SysTenantApi)
			.apiSysTenantGoTenantPost({ id: row.id })
			.then(res => reLoadLoginAccessToken(res.data.result))
	);
};

// 切换租户
const changeTenant = (row: any) => {
	ElMessageBox.confirm(`确定要将当前用户切换到【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(() =>
		getAPI(SysTenantApi)
			.apiSysTenantChangeTenantPost({ id: row.id })
			.then(res => reLoadLoginAccessToken(res.data.result))
	);
};

const syncGrantMenu = (row: any) => {
	ElMessageBox.confirm(`确定要将同步【${row.name}】的授权数据?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		await getAPI(SysTenantApi).apiSysTenantSyncGrantMenuPost({ id: row.id });
		ElMessage.success('同步授权成功');
	});
};

// 打开新增页面
const openAddTenant = () => {
	state.editTenantTitle = '添加租户';
	editTenantRef.value?.openDialog({ tenantType: 0, orderNo: 100, host: '' });
};

// 打开编辑页面
const openEditTenant = (row: any) => {
	state.editTenantTitle = '编辑租户';
	editTenantRef.value?.openDialog(row);
};

// 打开授权菜单页面
const openGrantMenu = async (row: any) => {
	grantMenuRef.value?.openDialog(row);
};

// 重置密码
const resetTenantPwd = async (row: any) => {
	ElMessageBox.confirm(`确定重置密码：【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysTenantApi)
				.apiSysTenantResetPwdPost({ userId: row.userId })
				.then((res) => {
					ElMessage.success(`密码重置成功为：${res.data.result}`);
				});
		})
		.catch(() => { });
};

// 删除
const delTenant = (row: any) => {
	ElMessageBox.confirm(`确定删除租户：【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysTenantApi).apiSysTenantDeletePost({ id: row.id });
			await handleAdvancedQuery([]);
			ElMessage.success('删除成功');
		})
		.catch(() => { });
};

// 改变页面容量
const handleSizeChange = async (val: number) => {
	state.tableParams.pageSize = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

// 改变页码序号
const handleCurrentChange = async (val: number) => {
	state.tableParams.page = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

// 创建租户库
const createTenant = (row: any) => {
	ElMessageBox.confirm(`确定创建/更新租户数据库：【${row.name}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysTenantApi).apiSysTenantCreateDbPost({ id: row.id });
			ElMessage.success('创建/更新租户数据库成功');
		})
		.catch(() => { });
};

// 修改状态
const changeStatus = (row: any) => {
	getAPI(SysTenantApi)
		.apiSysTenantSetStatusPost({ id: row.id, status: row.status })
		.then(() => {
			ElMessage.success('租户状态设置成功');
		})
		.catch(() => {
			row.status = row.status == 1 ? 2 : 1;
		});
};
</script>

<style scoped lang="scss">
.sys-tenant-card {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>