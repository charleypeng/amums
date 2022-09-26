/* tslint:disable */
/* eslint-disable */
/**
 * Admin.NET
 * 让 .NET 开发更简单、更通用、更流行。前后端分离架构(.NET6/Vue3)，开箱即用紧随前沿技术。<br/><a href='https://gitee.com/zuohuaijun/Admin.NET/'>https://gitee.com/zuohuaijun/Admin.NET</a>
 *
 * OpenAPI spec version: 1.0.0
 * Contact: 515096995@qq.com
 *
 * NOTE: This class is auto generated by the swagger code generator program.
 * https://github.com/swagger-api/swagger-codegen.git
 * Do not edit the class manually.
 */
/**
 * 
 * @export
 * @interface AddRoleInput
 */
export interface AddRoleInput {
    /**
     * 主键Id
     * @type {number}
     * @memberof AddRoleInput
     */
    id: number;
    /**
     * 编码
     * @type {string}
     * @memberof AddRoleInput
     */
    code?: string | null;
    /**
     * 排序
     * @type {number}
     * @memberof AddRoleInput
     */
    order?: number;
    /**
     * 数据范围类型
     * @type {number}
     * @memberof AddRoleInput
     */
    dataScope?: number;
    /**
     * 备注
     * @type {string}
     * @memberof AddRoleInput
     */
    remark?: string | null;
    /**
     * 状态
     * @type {number}
     * @memberof AddRoleInput
     */
    status?: number;
    /**
     * 名称
     * @type {string}
     * @memberof AddRoleInput
     */
    name: string;
}