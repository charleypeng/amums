<template>
  <div>
    <a-row :gutter="20">
      <a-col :span="2" class="leftboard">
        <a-card :bordered="false" style="height: 700px">
          <a-radio-group size="small" >
            <a-radio-button value="add" @click="zoomAdd" v-show="false">
              <a-icon type="plus-circle" />
            </a-radio-button>
            <a-radio-button value="minus" style="margin-left:5px" @click="zoomSub" v-show="false">
              <a-icon type="minus-circle" />
            </a-radio-button>
          </a-radio-group>
          <a-divider />
          <draggable
            :list="boardlist"
            :group="{ name: 'board', pull: 'clone', put: false }"
            @change="log"
            @end="onEnd"
            :options="draggableOptions"
            style="margin-top: 60px;">
            <template v-for="(node, index) in nodesourcelist">
              <div :id="node.key" style="margin-top:20px; margin-left:-5px" :key="index">
                <a-button :type="node.type" class="itembutton">
                  {{ node.title }}
                </a-button>
              </div>
            </template>
          </draggable>
        </a-card>
      </a-col>
      <a-col :span="16">
        <a-card :bordered="true" style="height: 700px;overflow-x: auto;">
          <div ref="efContainer" id="diagramContainer" class="nodeboard">
            <template v-for="(node, index) in value">
              <flow-nodeshow
                :id="node.key"
                :key="index"
                :node="node"
                :tabValue="tabValue"
                @nodeclick="setnode(node)"
                @changeNodeSite="changeNodeSite"
                :currentNode="currentNode"></flow-nodeshow>
            </template>
          </div>
        </a-card>
      </a-col>
      <a-col :span="6" class="rightboard">
        <a-card :bordered="false" style="height: 700px">
          <node-property
            :nodelist="value"
            :formlistsource="formlistsource"
            ref="nodeproperty"
            :currentNode="currentNode"
            :conditionNode="conditionNode"
            :isclickLine="isclickLine"
            :activekey="tabValue"
            :formId="formId"
            @changeNextNode="changeNextNode"
            @directionConnection="directionConnection"
            @nextStepConnection="nextStepConnection"
            @renameConnection="renameConnection"
            @deleteConnection="deleteConnection"
            @deleteNode="removeNode"
          ></node-property>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script>
import NodeProperty from './store/nodeProperty.vue'
import { createconditionFlowNodeDetail } from './store/conditionflownode'
// import { createConditionsDetail } from './store/conditions'
import { sourcenodes } from './store/sourcenodes'
import FlowNodeshow from './store/flowNodeGroup'
import { jsPlumb } from 'jsplumb'
import draggable from 'vuedraggable'
export default {
  name: 'Home1',
  components: {
    NodeProperty,
    draggable,
    FlowNodeshow
  },
   props: {
     // ????????????????????????????????????flow
    localflow: {
      type: Array,
      default: null
    },
    propformId: {
      type: Number,
      default: null
    }
  },
  data() {
    return {
        zoom: 1,
        formId: this.propformId,
        draggableOptions: {
        preventOnFilter: false,
        sort: false,
        disabled: false,
        ghostClass: 'tt',
        // ?????????H5???????????????
        forceFallback: true
        // ?????????????????????
        // fallbackClass: 'flow-node-draggable'
                },
      positionchange: { id: '', position: [] },
      // ??????
      common: {
          isSource: true, // ???????????????????????????????????????
          isTarget: true,
          labelStyle: { cssClass: 'flowLabel' },
          Endpoint: ['Dot', { radius: 5, cssClass: 'ef-dot', hoverClass: 'ef-dot-hover' }],
          connector: ['Flowchart'], // ???????????????     Bezier: ???????????????  Flowchart: ??????90???????????????????????? StateMachine: ????????? Straight: ??????
          connectorStyle: { outlineStroke: '#3399FF', strokeWidth: 1 }, // ????????????????????????????????????????????????
          maxConnections: -1, // ????????????????????????-1????????????
          connectorHoverStyle: { strokeWidth: 3, outlineStroke: 'red' },
         // Container: 'diagramContainer',
          // overlays: [['Arrow', { width: 12, length: 12, location: 0.5 }]] // ?????? ??????
          connectorOverlays: [
            [
              'Arrow',
              {
                width: 10,
                length: 10,
                location: 0.8
              }
            ]
          ],
          paintStyle: {
          fill: 'white',
          outlineStroke: 'orange',
          strokeWidth: 2
          },
          hoverPaintStyle: {
           outlineStroke: 'lightblue'
          }
      },
      formlistsource: [],
      plumbIns: null,
      // ??????????????????
      tabValue: 'node',
      // ?????????????????????
      isclickLine: false,
      // ?????????????????????
      currentConnection: null,
      // ????????????
      currentNode: null,
      // ????????????
      conditionNode: null,
      nodesourcelist: [],
      value: [],
      testnode: null,
      conditionflownode: {
        label: '',
        nodeId: '',
        conditions: {
          field: '',
          operator: '',
          value: ''
        }
      },
      WorkflowDefinition: {
        color: '#2d8cf0',
        version: 1,
        nodes: null
      },
      boardlist: []
    }
  },
  created() {
    this.nodesourcelist = sourcenodes
     },
  mounted() {
    this.plumbIns = jsPlumb.getInstance(this.common)
    this.plumbIns.importDefaults({
          // ????????????????????????????????????????????????
           ConnectionsDetachable: false
        })
    this.$nextTick(() => {
      this.plumbIns.ready(() => {
        this.plumbIns.bind('connection', this.onConnection)
        this.plumbIns.bind('click', this.onClickConnection)
        this.plumbIns.bind('connectionMoved', this.onMoved)
        // this.plumbIns.setContainer(this.$refs.efContainer)
        if (this.localflow) {
          this.value = this.localflow
          if (this.value.length > 0) {
            this.initWorkflow()
          }
        }
      })
    })
  },
  computed: {},
  methods: {
    // ??????
     zoomAdd () {
      if (this.zoom >= 1) {
          return
      }
      this.zoom = this.zoom + 0.1
      this.$refs.efContainer.style.transform = `scale(${this.zoom})`
      this.plumbIns.setZoom(this.zoom)
    },
    zoomSub () {
      if (this.zoom <= 0) {
          return
      }
      this.zoom = this.zoom - 0.1
      this.$refs.efContainer.style.transform = `scale(${this.zoom})`
      this.plumbIns.setZoom(this.zoom)
    },
    // ????????????
    removeNode() {
      this.value.forEach((node) => {
      node.nextNodes = node.nextNodes.filter(
        (u) => u.nodeId !== this.currentNode.key
      )
      node.parentNodes = node.parentNodes.filter(
        (u) => u !== this.currentNode.key
      )
    })
    this.plumbIns.remove(this.currentNode.key)
    this.value.filter(i => i.key === this.currentNode.key)[0].enable = false
    },
    onMoved() {
    },
    // ????????????????????????flow
    initWorkflow() {
    this.value.forEach(item => {
      this.$nextTick(() => {
        this.addNode(item)
      })
      setTimeout(() => {
      item.nextNodes.forEach((nnode) => {
      this.plumbIns.connect({
      uuids: [nnode.source, nnode.target]
       })
      })
      })
      }, 10000)
     },
    // ?????????????????????
    changeNodeSite (data) {
        this.value.filter(u => u.key === data.nodeId)[0].position = [data.left.substr(0, data.left.length - 2), data.top.substr(0, data.top.length - 2)]
    },
    // ??????????????????
    renameConnection(label) {
         var conn = this.plumbIns.getConnections({
        source: this.currentConnection.sourceId,
        target: this.currentConnection.targetId
      })[0]
      if (this.currentConnection !== null) {
        conn.setLabel(label)
      }
    },
    changeNextNode(parntsNodeID) {

    },
    directionConnection(direction) {
     this.currentNode.direction = direction
    },
    nextStepConnection(nextstep) {
     this.currentNode.nextStep = nextstep
    },
    getNode(key) {
      return this.value.filter(u => u.key === key)[0]
    },
    // ????????????
    deleteConnection() {
      const source = this.getNode(this.currentConnection.sourceId)
      const target = this.getNode(this.currentConnection.targetId)
      source.nextNodes = source.nextNodes.filter(u => u.nodeId !== this.currentConnection.targetId)
      target.parentNodes = source.parentNodes.filter(u => u !== this.currentConnection.sourceId)
      this.plumbIns.deleteConnection(this.currentConnection)
    },
    // ????????????
    setnode(node) {
      this.$refs.nodeproperty.gettempStepBodyName()
      this.isclickLine = false
      this.tabValue = 'node'
      this.currentNode = node
      if (this.currentNode.stepBody && this.currentNode.stepBody.name) {
      }
    },
    // ??????????????????
    onClickConnection(connection) {
      var sourse = this.value.filter(u => u.key === connection.sourceId)[0]
      this.conditionNode = sourse.nextNodes.filter(u => u.nodeId === connection.targetId)[0]
      this.currentConnection = connection
      this.isclickLine = true
      this.tabValue = 'line'
    },
    // ????????????
    onConnection(info) {
      var sourse = this.value.filter(u => u.key === info.sourceId)[0]
      var target = this.value.filter(u => u.key === info.targetId)[0]
      if (target.parentNodes.filter(u => u === sourse.key).length <= 0) {
        target.parentNodes.push(sourse.key)
      }
      var sourceuuid = sourse.endpointOptions.filter(u => u.anchor === info.sourceEndpoint.anchor.type)[0].uuid
      var targetuuid = target.endpointOptions.filter(u => u.anchor === info.targetEndpoint.anchor.type)[0].uuid
      if (sourse.nextNodes.filter(u => u.nodeId === target.key).length <= 0) {
          const c = createconditionFlowNodeDetail(target.key, sourceuuid, targetuuid)
        sourse.nextNodes.push(c)
      }
    },
    createNodeByType(type, x, y, key) {
      const node = JSON.parse(JSON.stringify(this.nodesourcelist.filter(u => u.key === type)[0]))
      node.key =
        key !== undefined
          ? key
          : node.key +
            '_' +
            Date.now() +
            Math.random()
              .toString(36)
              .substr(2)
      if (node.endpointOptions !== null) {
          node.endpointOptions.forEach(option => {
          option.uuid = node.key + option.anchor
        })
      }
      node.position = [x, y]
           return node
    },
    addNodeByType(type, x, y) {
      if (type === 'start' && this.value.filter(i => i.key.slice(0, 5) === type && i.enable === true).length > 0) {
        this.$message.info('???????????????????????????????????????')
        return
      }
      const node = this.createNodeByType(type, x, y)
      this.value.push(node)
      this.$nextTick(() => {
        this.addNode(node)
      })
    },
    // ????????????
    revalidate() {
      this.$nextTick(() => {
        this.plumbIns.revalidate(this.currentNode.key)
      })
    },
    // ????????????
    addNode(node) {
      this.plumbIns.ready(() => {
      if (node.endpointOptions !== null) {
          node.endpointOptions.forEach(option => {
          this.plumbIns.addEndpoint(node.key, option, this.common)
          })
        }
        // ?????????   containment?????????????????? grid????????????????????????????????????
        this.plumbIns.draggable(node.key, { containment: 'diagramContainer',
            stop: function (el) {
        },
        grid: [10, 10] })
      })
    },
    log(evt) {},
    onEnd(evt) {
      var efContainer = this.$refs.efContainer
      var containerRect = efContainer.getBoundingClientRect()
      var left = evt.originalEvent.clientX - containerRect.x + efContainer.scrollLeft - 80
      var top = evt.originalEvent.clientY - containerRect.y + efContainer.scrollTop - 30
      if (left > 0 && top > 0) {
        this.addNodeByType(evt.item.id, left, top)
      }
    },
    returnnode() {
      this.WorkflowDefinition.nodes = this.value.filter(i => i.enable === true)
    },
    show() {
    }
  }
}
</script>
<style scoped>
/* ????????????label ??????*/
.jtk-overlay.flowLabel:not(.aLabel) {
    padding: 4px 10px;
    background-color: rgb(15, 208, 241);
    color: #242525 !important;
    border: 1px solid #E0E3E7;
    border-radius: 5px;
}
.jtk-overlay {
    cursor: pointer;
    color: #4A4A4A;
}
.nodeboard {
  width: 3000px;
  height: auto;
  margin-top: 20px;
  min-height: 600px;
  /* ???????????? */
  background:
     linear-gradient(to right,rgb(245, 240, 240) 1px,transparent 1px),
     linear-gradient(to bottom,rgb(245, 240, 240) 1px,transparent 1px);
     background-repeat: repeat;/* ????????? repeat */
     background-size: 20px 20px;
}
.leftboard {
  height: 700px;
  /* background-color: rgb(181, 225, 226); */
}
.rightboard {
  height: 700px;
  /* background-color: rgb(200, 233, 146); */
}
.ef-dot {
    background-color: #d31020;
    border-radius: 10px;
}

.ef-node-menu-li {
    color: #565758;
    width: 150px;
    border: 1px dashed #E0E3E7;
    margin: 5px 0 5px 0;
    padding: 5px;
    border-radius: 5px;
    padding-left: 8px;
}

.itembutton:hover{
    cursor: move;
    border: 3px dashed #1879FF;
}
.ef-node-menu-li:hover {
    /* ??????????????????*/
    cursor: move;
    background-color: #F0F7FF;
    border: 1px dashed #1879FF;
    border-left: 4px solid #1879FF;
    padding-left: 5px;
}
</style>
