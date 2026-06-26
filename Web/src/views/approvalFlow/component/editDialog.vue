<template>
	<div class="labApprovalFlow-container">
		<el-dialog v-model="state.isShowDialog" :close-on-click-modal="false" fullscreen class="flow-dialog">
			<template #header>
				<div class="dialog-header">
					<el-icon size="16" style="margin-right: 3px"> <ele-Edit /> </el-icon>
					<span>{{ props.title }}</span>
				</div>
			</template>
			<div class="design-container">
				<!-- 左侧基本信息 -->
				<div class="design-left">
					<div class="design-left-title">基本信息</div>
					<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" :rules="rules" size="default">
						<el-form-item v-show="false">
							<el-input v-model="state.ruleForm.id" />
						</el-form-item>
						<el-form-item label="编号" prop="code">
							<el-input v-model="state.ruleForm.code" placeholder="留空自动生成" maxlength="32" clearable />
						</el-form-item>
						<el-form-item label="名称" prop="name" :rules="[{ required: true, message: '名称不能为空', trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name" placeholder="请输入流程名称" maxlength="32" clearable />
						</el-form-item>
						<el-form-item label="状态" prop="status">
							<el-select v-model="state.ruleForm.status" placeholder="请选择状态">
								<el-option label="启用" :value="1" />
								<el-option label="禁用" :value="0" />
							</el-select>
						</el-form-item>
						<el-form-item label="备注" prop="remark">
							<el-input v-model="state.ruleForm.remark" placeholder="请输入备注" type="textarea" :rows="2" maxlength="255" clearable />
						</el-form-item>
					</el-form>

					<!-- 节点面板 -->
					<div class="design-left-title" style="margin-top: 10px">流程节点</div>
					<div class="node-panel">
						<div
							class="node-item"
							v-for="node in nodeTypes"
							:key="node.type"
							@mousedown="(e) => onNodeMouseDown(e, node)"
						>
							<div class="node-icon" :style="{ background: node.color }">{{ node.icon }}</div>
							<span>{{ node.label }}</span>
						</div>
					</div>

					<!-- 工具栏 -->
					<div class="design-left-title" style="margin-top: 10px">操作</div>
					<div class="toolbar">
						<el-button size="small" @click="zoomIn">放大</el-button>
						<el-button size="small" @click="zoomOut">缩小</el-button>
						<el-button size="small" @click="zoomReset">重置</el-button>
						<el-button size="small" @click="viewData">查看数据</el-button>
						<el-button size="small" @click="undo">撤销</el-button>
						<el-button size="small" @click="redo">重做</el-button>
					</div>
				</div>

				<!-- 右侧画布 -->
				<div class="design-right" ref="canvasContainerRef">
					<div class="flow-canvas" id="flow-canvas"></div>
					<el-drawer v-model="state.showProperty" title="节点属性" direction="rtl" size="400px" :before-close="closeProperty">
						<PropertyDialog v-if="state.showProperty" :nodeData="state.nodeData" :lf="lf" @setPropertiesFinish="closeProperty" />
					</el-drawer>
				</div>
			</div>
			<template #footer>
				<div class="dialog-footer-bar">
					<el-button @click="cancel">取 消</el-button>
					<el-button type="primary" @click="submit" :loading="state.loading">确 定</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- 数据查看弹窗 -->
		<el-dialog v-model="state.showData" title="流程数据" width="60%">
			<PanelDataDialog :graphData="state.graphData" />
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { reactive, ref, nextTick } from 'vue';
import { ElMessage } from 'element-plus';
import type { FormRules } from 'element-plus';

import LogicFlow from '@logicflow/core';
import { BpmnElement, InsertNodeInPolyline, Menu, MiniMap, SelectionSelect, Snapshot } from '@logicflow/extension';
import '@logicflow/core/dist/index.css';
import '@logicflow/extension/lib/style/index.css';

import RegisterEdge from './LogicFlow/Register/RegisterEdge';
import RegisterNode from './LogicFlow/Register/RegisterNode';
import PanelDataDialog from './LogicFlow/Panel/PanelDataDialog.vue';
import PropertyDialog from './LogicFlow/Property/PropertyDialog.vue';

import { getAPI } from '/@/utils/axios-utils';
import { ApprovalFlowApi } from '/@/api-plugins/approvalFlow/api';

const nodeTypes = [
	{ type: 'start', label: '开始', icon: '○', color: '#67C23A' },
	{ type: 'approval', label: '审批', icon: '审', color: '#409EFF' },
	{ type: 'gateway', label: '网关', icon: '◇', color: '#E6A23C' },
	{ type: 'cc', label: '抄送', icon: '抄', color: '#909399' },
	{ type: 'end', label: '结束', icon: '●', color: '#F56C6C' },
];

var props = defineProps({
	title: {
		type: String,
		default: '',
	},
});

const emit = defineEmits(['reloadTable']);

const ruleFormRef = ref();
const canvasContainerRef = ref();
const lf = ref<InstanceType<typeof LogicFlow>>();

const state = reactive({
	loading: false,
	isShowDialog: false,
	ruleForm: {} as any,
	showProperty: false,
	nodeData: {} as any,
	showData: false,
	graphData: {} as any,
});

const rules = ref<FormRules>({
	name: [
		{
			pattern: /^(?!^[0-9].*$).*/,
			message: '不能以数字开头',
			trigger: 'blur',
		},
	],
});

const openDialog = async (row: any) => {
	let rowData = JSON.parse(JSON.stringify(row));
	if (rowData.id) {
		state.ruleForm = (await getAPI(ApprovalFlowApi).apiApprovalFlowDetailGet(rowData.id)).data.result;
	} else {
		state.ruleForm = { ...rowData, status: rowData.status ?? 1 };
	}
	state.isShowDialog = true;
	nextTick(() => {
		setTimeout(() => {
			initGraph();
		}, 100);
	});
};

const closeDialog = () => {
	emit('reloadTable');
	state.isShowDialog = false;
};

const cancel = () => {
	state.isShowDialog = false;
};

const initGraph = () => {
	if (lf.value) {
		lf.value.destroy();
		lf.value = undefined;
	}

	const container = document.getElementById('flow-canvas');
	if (!container) return;

	// 获取容器实际尺寸
	const containerRect = canvasContainerRef.value?.getBoundingClientRect();
	const width = containerRect?.width || container.clientWidth || 800;
	const height = containerRect?.height || container.clientHeight || 600;

	// 设置容器尺寸
	container.style.width = width + 'px';
	container.style.height = height + 'px';

	const config = {
		stopScrollGraph: false,
		stopZoomGraph: false,
		metaKeyMultipleSelected: true,
		grid: { size: 10, type: 'dot' },
		keyboard: { enabled: true },
		snapline: true,
	};

	lf.value = new LogicFlow({
		...config,
		plugins: [BpmnElement, InsertNodeInPolyline, Menu, MiniMap, SelectionSelect, Snapshot],
		container: container,
		width: width,
		height: height,
	});

	lf.value.setTheme({
		snapline: { stroke: '#1E90FF', strokeWidth: 1 },
	});

	RegisterNode.Register(lf.value);
	RegisterEdge.Register(lf.value);

	lf.value.on('node:click', ({ data }: any) => {
		state.nodeData = data;
		state.showProperty = true;
	});

	lf.value.on('edge:click', ({ data }: any) => {
		state.nodeData = data;
		state.showProperty = true;
	});

	// 渲染已有流程数据
	let flowData = { nodes: [], edges: [] };
	if (state.ruleForm?.flowJson) {
		try {
			flowData = JSON.parse(state.ruleForm.flowJson);
		} catch (e) {
			console.error('解析流程数据失败', e);
		}
	}

	lf.value.render(flowData);
	lf.value.focusOn({ coordinate: { x: 300, y: 200 } });
};

// 节点拖拽到画布 - 使用 mousedown + mousemove + mouseup 实现
const onNodeMouseDown = (e: MouseEvent, node: any) => {
	e.preventDefault();
	if (!lf.value) return;

	const canvas = document.getElementById('flow-canvas');
	if (!canvas) return;

	const canvasRect = canvas.getBoundingClientRect();
	const startX = e.clientX;
	const startY = e.clientY;
	let isMoved = false;

	const onMouseMove = (moveE: MouseEvent) => {
		const dx = moveE.clientX - startX;
		const dy = moveE.clientY - startY;
		if (Math.abs(dx) > 5 || Math.abs(dy) > 5) {
			isMoved = true;
		}
	};

	const onMouseUp = (upE: MouseEvent) => {
		document.removeEventListener('mousemove', onMouseMove);
		document.removeEventListener('mouseup', onMouseUp);

		if (isMoved && lf.value) {
			// 计算在画布中的位置
			const x = upE.clientX - canvasRect.left;
			const y = upE.clientY - canvasRect.top;

			// 添加节点到画布
			lf.value.addNode({
				type: node.type,
				x: x,
				y: y,
				text: node.label,
			});
		}
	};

	document.addEventListener('mousemove', onMouseMove);
	document.addEventListener('mouseup', onMouseUp);
};

const submit = async () => {
	ruleFormRef.value.validate(async (isValid: boolean, fields?: any) => {
		if (isValid) {
			state.loading = true;
			try {
				// 保存流程数据
				if (lf.value) {
					state.ruleForm.flowJson = JSON.stringify(lf.value.getGraphData());
				}

				if (state.ruleForm.id == undefined || state.ruleForm.id == null || state.ruleForm.id == 0) {
					await getAPI(ApprovalFlowApi).apiApprovalFlowAddPost(state.ruleForm);
					ElMessage.success('新增成功');
				} else {
					await getAPI(ApprovalFlowApi).apiApprovalFlowUpdatePost(state.ruleForm);
					ElMessage.success('更新成功');
				}
				closeDialog();
			} catch (e) {
				ElMessage.error('操作失败');
			}
			state.loading = false;
		} else {
			ElMessage({
				message: `表单有${Object.keys(fields).length}处验证失败，请修改后再提交`,
				type: 'error',
			});
		}
	});
};

const closeProperty = () => {
	state.showProperty = false;
};

const zoomIn = () => lf.value?.zoom(true);
const zoomOut = () => lf.value?.zoom(false);
const zoomReset = () => lf.value?.resetZoom();
const undo = () => lf.value?.undo();
const redo = () => lf.value?.redo();

const viewData = () => {
	if (lf.value) {
		state.graphData = lf.value.getGraphData();
		state.showData = true;
	}
};

defineExpose({ openDialog });
</script>

<style lang="scss">
.flow-dialog {
	margin: 0 !important;
	
	.el-dialog__header {
		padding: 10px 20px !important;
		margin: 0 !important;
		border-bottom: 1px solid #e4e7ed;
		background: #fff;
	}

	.el-dialog__footer {
		padding: 10px 20px !important;
		margin: 0 !important;
		border-top: 1px solid #e4e7ed;
		background: #fff;
		position: absolute;
		bottom: 0;
		left: 0;
		right: 0;
		z-index: 10;
	}

	.el-dialog__body {
		padding: 0 !important;
		margin: 0 !important;
		overflow: hidden;
		position: absolute;
		top: 42px;
		bottom: 42px;
		left: 0;
		right: 0;
	}
}

.dialog-header {
	color: #303133;
	font-size: 15px;
	font-weight: 600;
	display: flex;
	align-items: center;
}

.dialog-footer-bar {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
}

.design-container {
	display: flex;
	height: 100%;
	width: 100%;

	.design-left {
		width: 260px;
		min-width: 260px;
		background: #f5f7fa;
		border-right: 1px solid #e4e7ed;
		padding: 12px;
		overflow-y: auto;
		box-sizing: border-box;

		.design-left-title {
			font-size: 13px;
			font-weight: 600;
			color: #303133;
			margin-bottom: 8px;
			padding-bottom: 6px;
			border-bottom: 1px solid #e4e7ed;
		}

		.node-panel {
			display: flex;
			flex-wrap: wrap;
			gap: 6px;

			.node-item {
				display: flex;
				align-items: center;
				gap: 6px;
				padding: 6px 10px;
				cursor: grab;
				border-radius: 4px;
				transition: all 0.2s;
				font-size: 12px;
				background: #fff;
				border: 1px solid #dcdfe6;
				user-select: none;

				&:hover {
					background: #ecf5ff;
					border-color: #b3d8ff;
					color: #409eff;
				}

				&:active {
					cursor: grabbing;
				}

				.node-icon {
					width: 24px;
					height: 24px;
					border-radius: 4px;
					display: flex;
					align-items: center;
					justify-content: center;
					color: #fff;
					font-size: 11px;
					font-weight: bold;
				}
			}
		}

		.toolbar {
			display: flex;
			flex-wrap: wrap;
			gap: 4px;
		}
	}

	.design-right {
		flex: 1;
		position: relative;
		background: #fff;
		overflow: hidden;

		.flow-canvas {
			width: 100%;
			height: 100%;
			position: absolute;
			top: 0;
			left: 0;
		}
	}
}
</style>
