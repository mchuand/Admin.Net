<template>
	<div class="sys-difflog-container">
		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysDifflog" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table :data="state.logData" style="width: 100%" v-loading="state.loading" border>
				<el-table-column type="expand">
					<template #default="scope">
						<el-card header="差异数据" style="width: 100%; margin: 5px">
							<el-table :data="item.columns" v-for="item in scope.row.diffData" :key="item.tableName" :span-method="(data: any) => diffTableSpanMethod(data, item)" border style="width: 100%">
								<el-table-column label="表名" width="200">
									<template #default>
										{{item.tableName}}
										<br/>
										{{item.tableDescription}}
									</template>
								</el-table-column>
								<el-table-column prop="columnName" label="字段描述" width="300" :formatter="(row: any) => `${row.columnName} - ${row.columnDescription}`" />
								<el-table-column prop="beforeValue" label="修改前" show-overflow-tooltip>
									<template #default="columnScope">
										<pre v-html="markDiff(columnScope.row.beforeValue, columnScope.row.afterValue, true)" />
									</template>
								</el-table-column>
								<el-table-column prop="afterValue" label="修改后" show-overflow-tooltip>
									<template #default="columnScope">
										<pre v-html="markDiff(columnScope.row.beforeValue, columnScope.row.afterValue, false)" />
									</template>
								</el-table-column>
							</el-table>
							<el-table :data="[ { sql: scope.row.sql } ]" border style="width: 100%">
								<el-table-column prop="sql" label="SQL语句">
									<template #default>
										<pre class="sql" v-html="formatSql(scope.row.sql)"></pre>
									</template>
								</el-table-column>
							</el-table>
							<el-table :data="scope.row.parameters" border style="width: 100%">
								<el-table-column prop="parameterName" label="参数名" width="200" />
								<el-table-column prop="typeName" label="类型" width="100" />
								<el-table-column prop="value" label="值" />
							</el-table>
						</el-card>
					</template>
				</el-table-column>
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="diffType" label="差异操作" header-align="center" show-overflow-tooltip />
				<el-table-column prop="elapsed" label="耗时(ms)" header-align="center" show-overflow-tooltip />
				<el-table-column prop="message" label="日志消息" header-align="center" show-overflow-tooltip />
				<el-table-column prop="businessData" label="业务对象" header-align="center" show-overflow-tooltip />
				<el-table-column prop="createTime" label="操作时间" align="center" show-overflow-tooltip />
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
	</div>
</template>

<script lang="ts" setup name="sysDiffLog">
import { onMounted, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { SysLogDiffApi, SysTenantApi } from '/@/api-services/api';
import { SysLogDiff } from '/@/api-services/models';
import { useUserInfo } from "/@/stores/userInfo";
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

const userStore = useUserInfo();
const searchRef = ref();
const state = reactive({
	loading: false,
	tenantList: [] as Array<any>,
	queryParams: {
		tenantId: undefined as number | undefined,
	},
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	logData: [] as Array<SysLogDiff>,
});

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '租户', prop: 'tenantId', type: 'select', options: [], visible: userStore.userInfos.accountType == 999 },
	{ label: '操作时间', prop: 'createTime', type: 'datetimeRange' },
];

// 关键字搜索字段列表
const keywordFields = ['diffType', 'message', 'businessData'];

onMounted(async () => {
	if (userStore.userInfos.accountType == 999) {
		state.tenantList = await getAPI(SysTenantApi).apiSysTenantListGet().then(res => res.data.result ?? []);
		
		const tenantField = searchFields.find(f => f.prop === 'tenantId');
		if (tenantField) {
			tenantField.options = state.tenantList.map(item => ({
				label: `${item.label} (${item.host})`,
				value: item.value
			}));
		}
		
		if (state.tenantList.length > 0) {
			state.queryParams.tenantId = state.tenantList[0].value;
			if (searchRef.value) {
				searchRef.value.setQueryParams({ tenantId: state.tenantList[0].value });
			}
		}
	}
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
		tenantId: state.queryParams.tenantId,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysLogDiffApi).apiSysLogDiffPageAdvancedPost(params as any);
		state.logData = res.data.result?.items ?? [];
		state.logData.forEach(e => {
			e.diffData = JSON.parse(e.diffData ?? "[]");
			e.parameters = JSON.parse(e.parameters ?? "[]");
		});
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

// 合并差异表格表名列
const diffTableSpanMethod = ({columnIndex, rowIndex}: any, itme: any) => {
	if (columnIndex === 0) {
		if (rowIndex === 0) {
			return { rowspan: itme.columns.length, colspan: 1 }
		} else {
			return { rowspan: 0, colspan: 0 }
		}
	}
}

const formatSql = (sql: string) => {
	let formatted = sql.replace(/\s+/g, ' ').trim();
	formatted = formatted.replace(/`([^`]+)`/g, '<span class="sql-backtick">`$1`</span>');
	formatted = formatted.replace(/(@\w+)/g, '<span class="sql-param">$1</span>');
	formatted = formatted.replace(/\b(INSERT|DELETE|UPDATE|SELECT|FROM|SET|JOIN|ON|AND|OR|IN|NOT|IS|NULL|WHERE|TRUE|FALSE|LIKE|ORDER BY|GROUP BY|HAVING|LIMIT|AS|WITH|CASE|WHEN|THEN|ELSE|END)\b/g, '<span class="sql-keyword">$1</span>');
	formatted = formatted.replace(/(SET|VALUES)(?=\s)/g, '$1\n    ');
	formatted = formatted.replace(/,(?![^]*?,\s*$)(?=[^\s])/g, ',\n    ');
	formatted = formatted.replace(/([\s\S]+)(WHERE)/g, '$1\n$2');
	formatted = formatted.replace(/\n\s*\n/g, '\n');
	return formatted;
};

function lcs(s1: string, s2: string): number[][] {
	const m = s1.length;
	const n = s2.length;
	const dp = Array.from({ length: m + 1 }, () => Array(n + 1).fill(0));
	for (let i = 1; i <= m; i++) {
		for (let j = 1; j <= n; j++) {
			if (s1[i - 1] === s2[j - 1]) {
				dp[i][j] = dp[i - 1][j - 1] + 1;
			} else {
				dp[i][j] = Math.max(dp[i - 1][j], dp[i][j - 1]);
			}
		}
	}
	return dp;
}

function markDiff(oldData: any, newData: any, returnOld: boolean): string {
	if (typeof oldData !== 'string' || typeof newData !== 'string') {
		return `<span class="diff-${returnOld ? 'delete' : 'add'}">${returnOld ? oldData : newData}</span>`;
	}
	const dp = lcs(oldData, newData);
	const m = oldData.length;
	const n = newData.length;
	let oldIndex = m, newIndex = n;
	const diffResult: { type: string, content: string }[] = [];
	while (oldIndex > 0 || newIndex > 0) {
		if (oldIndex > 0 && newIndex > 0 && oldData[oldIndex - 1] === newData[newIndex - 1]) {
			diffResult.push({ type: 'unchanged', content: oldData[oldIndex - 1] });
			oldIndex--;
			newIndex--;
		} else if (newIndex > 0 && (oldIndex === 0 || dp[oldIndex][newIndex - 1] >= dp[oldIndex - 1][newIndex])) {
			diffResult.push({ type: 'add', content: newData[newIndex - 1] });
			newIndex--;
		} else {
			diffResult.push({ type: 'delete', content: oldData[oldIndex - 1] });
			oldIndex--;
		}
	}
	const result = diffResult.reverse().map(chunk => {
		switch (chunk.type) {
			case 'add': return `<span class="diff-add">${chunk.content}</span>`;
			case 'delete': return `<span class="diff-delete">${chunk.content}</span>`;
			default: return chunk.content;
		}
	}).join('');
	return result.replace(returnOld ? /<span class="diff-add">(.*?)<\/span>/g : /<span class="diff-delete">(.*?)<\/span>/g, '');
}
</script>

<style scoped lang="scss">
.sys-difflog-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}

:deep(pre.sql) {
	white-space: pre-wrap;
	.sql-param { color: green; }
	.sql-keyword { color: blue; }
	.sql-backtick { color: blueviolet; }
	span.diff-unchanged { color: inherit; }
	span.diff-delete { color: red; }
	span.diff-add { color: green; }
}

:deep(pre) {
	span.diff-delete { color: red; }
	span.diff-add { color: green; }
}
</style>