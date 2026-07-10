<template>
	<div class="weChatPay-container">

		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysWechatPay" :buttonConfig="wechatPayButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysWechatPay" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.tableData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="outTradeNumber" label="商户订单号" width="180"></el-table-column>
				<el-table-column prop="transactionId" label="支付订单号" width="220"></el-table-column>
				<el-table-column prop="description" label="描述" width="180"></el-table-column>
				<el-table-column prop="total" :formatter="amountFormatter" label="金额" width="70"></el-table-column>
				<el-table-column prop="tradeState" label="状态" width="70">
					<template #default="scope">
						<el-tag v-if="scope.row.tradeState == 'SUCCESS'" type="success"> 完成 </el-tag>
						<el-tag v-else-if="scope.row.tradeState == 'REFUND'" type="danger"> 退款 </el-tag>
						<el-tag v-else type="info"> 未完成 </el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="attachment" label="附加信息" width="180"></el-table-column>
				<el-table-column prop="tags" label="业务类型" width="90"></el-table-column>
				<el-table-column prop="createTime" label="创建时间" width="150"></el-table-column>
				<el-table-column prop="successTime" label="完成时间" width="150"></el-table-column>
				<el-table-column prop="businessId" label="业务ID" width="130"></el-table-column>
				<el-table-column label="操作" align="center" fixed="right">
					<template #default="scope">
						<el-button
              text
							size="small"
							type="primary"
							v-if="scope.row.qrcodeContent != null && scope.row.qrcodeContent != '' && (scope.row.tradeState === '' || !scope.row.tradeState)"
							@click="openQrDialog(scope.row.qrcodeContent)"
							>付款二维码</el-button
						>
						<el-button size="small" text type="primary" v-if="scope.row.tradeState === 'REFUND'" @click="openRefundDialog(scope.row.transactionId)">查看退款</el-button>
						<el-button size="small" text type="primary" v-if="scope.row.tradeState === 'SUCCESS'" @click="doRefund(scope.row)">全额退款</el-button>
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

		<el-dialog v-model="showAddDialog">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span>新增模拟数据</span>
				</div>
			</template>
			<el-form>
				<el-form-item label="商品">
					<el-input v-model="addData.description" placeholder="必填" clearable />
				</el-form-item>
				<el-form-item label="金额(分)">
					<el-input v-model="addData.total" placeholder="必填，填数字,单位是分" clearable />
				</el-form-item>
				<el-form-item label="附加信息">
					<el-input v-model="addData.attachment" clearable />
				</el-form-item>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="closeAddDialog">取 消</el-button>
					<el-button type="primary" @click="saveData">确 定</el-button>
				</span>
			</template>
		</el-dialog>
		<el-dialog v-model="showQrDialog">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-View /> </el-icon>
					<span>付款二维码</span>
				</div>
			</template>
			<div ref="qrDiv"></div>
		</el-dialog>

		<el-dialog v-model="showRefundDialog">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Document /> </el-icon>
					<span>退款信息</span>
				</div>
			</template>
			<el-table :data="subTableData" style="width: 100%" tooltip-effect="light" row-key="id" border>
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="outRefundNumber" label="商户退款号" width="180"></el-table-column>
				<el-table-column prop="transactionId" label="支付订单号" width="220"></el-table-column>
				<el-table-column prop="refund" label="金额(分)" width="70"></el-table-column>
				<el-table-column prop="reason" label="退款原因" width="180"></el-table-column>
				<el-table-column prop="tradeState" label="状态" width="70">
					<template #default="scope">
						<el-tag v-if="scope.row.tradeState == 'SUCCESS'" type="success"> 完成 </el-tag>
						<el-tag v-else-if="scope.row.tradeState == 'REFUND'" type="danger"> 退款 </el-tag>
						<el-tag v-else type="info"> 未完成 </el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="remark" label="备注" width="180"></el-table-column>
				<el-table-column prop="createTime" label="创建时间" width="150"></el-table-column>
				<el-table-column prop="successTime" label="完成时间" width="150"></el-table-column>
			</el-table>
		</el-dialog>
	</div>
</template>

<script setup lang="ts" name="weChatPay">
import { ref, nextTick, onMounted, reactive, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import QRCode from 'qrcodejs2-fixes';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { getAPI } from '/@/utils/axios-utils';
import { SysWechatPayApi } from '/@/api-services/api';
import { SysWechatPay } from '/@/api-services/models';

const qrDiv = ref<HTMLElement | null>(null);
const showAddDialog = ref(false);
const showQrDialog = ref(false);
const showRefundDialog = ref(false);
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

const subTableData = ref<any>([]);
const addData = ref<any>({});

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '订单号', prop: 'keyword', type: 'string' },
	{ label: '创建时间', prop: 'createTime', type: 'dateRange' },
];

// 关键字搜索字段列表
const keywordFields = ['outTradeNumber', 'transactionId'];

// 表格选中
const handleRowClick = (row: any) => {
	const table = tableRef.value;
	if (!table) return;
	table.toggleRowSelection(row);
};

const handleSelectionChange = (rows: any[]) => {
	selectRows.value = rows;
};

const state = reactive({
	loading: false,
	tableData: [] as Array<SysWechatPay>,
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editTenantTitle: '',
});

// 按钮栏配置
const wechatPayButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增模拟数据', icon: 'ele-Plus', color: 'primary' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddDialog(); break;
	}
};

// 校验选中行
const validateSelection = (minCount = 1, maxCount?: number): boolean => {
	if (selectRows.value.length < minCount) {
		ElMessage.warning(`请至少选择${minCount}条记录`);
		return false;
	}
	if (maxCount && selectRows.value.length > maxCount) {
		ElMessage.warning(`最多选择${maxCount}条记录`);
		return false;
	}
	return true;
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
		let res = await getAPI(SysWechatPayApi).apiSysWechatPayPageAdvancedPost(params as any);
		state.tableData = res.data.result?.items ?? [];
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

// 退款
const doRefund = async (orderInfo: any) => {
	ElMessageBox.prompt(`确定进行退款：${orderInfo.total / 100}元？请输入退款理由`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
	})
		.then(async ({ value }) => {
			let resp = await getAPI(SysWechatPayApi).apiSysWechatPayRefundDomesticPost({
				tradeId: orderInfo.outTradeNumber,
				reason: value,
				refund: orderInfo.total,
				total: orderInfo.total,
			} as any);
			if (resp.data.code == 200) {
				ElMessage.success(`【${value}】退款申请成功`);
			} else {
				ElMessage.error('操作失败：' + resp.data.message);
			}
		})
		.catch(() => {
			ElMessage.error('取消操作');
		});
};

const amountFormatter = (row: any, column: any, cellValue: number, index: number) => {
	return (cellValue / 100).toFixed(2);
};

// 打开新增页面
const openAddDialog = () => {
	addData.value = {
		description: null,
		total: null,
		attachment: null,
	};
	showAddDialog.value = true;
};

// 关闭新增页面
const closeAddDialog = () => {
	showAddDialog.value = false;
};

// 打开扫码页面
const openQrDialog = (code: string) => {
	showQrDialog.value = true;
	nextTick(() => {
		(<HTMLElement>qrDiv.value).innerHTML = '';
		new QRCode(qrDiv.value, {
			text: code,
			width: 260,
			height: 260,
			colorDark: '#000000',
			colorLight: '#ffffff',
		});
	});
};

// 打开退款页面
const openRefundDialog = async (code: string) => {
	var res = await getAPI(SysWechatPayApi).apiSysWechatPayListRefundPost(code as any);
	if (res.data.code === 200) {
		let tmpRows = res.data.result ?? [];
		subTableData.value = tmpRows;
		showRefundDialog.value = true;
	} else {
		ElMessage.error('获取退款列表失败，' + res.data.message);
	}
};

// 保存数据
const saveData = async () => {
	var res = await getAPI(SysWechatPayApi).apiSysWechatPayPayTransactionNativePost(addData.value);
	if (res.data.code === 200) {
		closeAddDialog();
		let code = res.data.result.qrcodeUrl;
		openQrDialog(code);
		handleAdvancedQuery(state.advancedConditions);
	} else {
		ElMessage.error('新建失败，' + res.data.message);
	}
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
</script>

<style scoped lang="scss">
.weChatPay-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
