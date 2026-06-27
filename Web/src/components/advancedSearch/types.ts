export enum CompareEnum {
	Eq = 0,
	Ne = 1,
	Lt = 2,
	Le = 3,
	Gt = 4,
	Ge = 5,
	Like = 6,
	NotLike = 7,
	In = 8,
	NotIn = 9,
	Between = 10,
	IsNull = 11,
	IsNotNull = 12
}

// 查询条件项（与后端 QueryConditionItem 保持一致）
export interface QueryCondition {
	field: string;       // 字段名
	value: any;          // 字段值
	compare: CompareEnum; // 比对方法
}

// 字段配置接口
export interface SearchField {
	label: string;
	prop: string;
	type?: 'string' | 'number' | 'numberRange' | 'date' | 'dateRange' | 'dic' | 'dicRange' | 'select';
	placeholder?: string;
	dicCode?: string;
	options?: { label: string; value: any }[];
	show?: boolean;
	defaultValue?: any;
	compare?: CompareEnum;
	required?: boolean;
}

export interface SettingField {
	label: string;
	prop: string;
	visible: boolean;
}
